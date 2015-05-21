using System;
using System.Linq;
using Automatonymous;
using Automatonymous.RepositoryBuilders;
using Automatonymous.RepositoryConfigurators;
using Automatonymous.Testing;
using MassTransit.Persistence.RavenDB.Tests.Framework;
using MassTransit.Saga;
using MassTransit.Testing;
using MassTransit.Testing.Scenarios;
using MassTransit.Testing.TestInstanceConfigurators;
using NUnit.Framework;
using Raven.Client;
using Raven.Imports.Newtonsoft.Json;

namespace MassTransit.Persistence.RavenDB.Tests.Automatonymous
{
    [TestFixture]
    public class When_using_RavenRespository
    {
        [Test]
        public void Should_have_a_saga()
        {
            ShoppingChore shoppingChore = _test.Saga.Created.Contains(_correlationId);
            Assert.IsNotNull(shoppingChore);

            ShoppingChore insatnce = null;
            do
            {
                insatnce = _repository.Where(chore => chore.CorrelationId == _correlationId).FirstOrDefault();
            } while (insatnce == null);
        }


        [Test]
        public void Should_have_a_saga_in_the_proper_state()
        {
            ShoppingChore shoppingChore = _test.Saga.ContainsInState(_correlationId, _machine.Final, _machine);

            foreach (ShoppingChore result in _repository.Select(x => x))
                Console.WriteLine("{0} - {1} ({2})", result.CorrelationId, result.CurrentState, result.Screwed);

            Assert.IsNotNull(shoppingChore);
        }

        [Test]
        public void Should_have_heard_girlfriend_yelling()
        {
            Assert.IsTrue(_test.Received.Any<GirlfriendYelling>());
        }

        [Test]
        public void Should_have_heard_her_yelling_to_the_end_of_the_world()
        {
            bool shoppingChore = _test.Saga.Created.Any(x => x.CorrelationId == _correlationId && x.Screwed);
            Assert.IsNotNull(shoppingChore);
        }

        [Test]
        public void Should_have_heard_the_impact()
        {
            Assert.IsTrue(_test.Received.Any<GotHitByACar>());
        }

        SuperShopper _machine;
        SagaTest<BusTestScenario, ShoppingChore> _test;
        IDocumentStore _sessionFactory;
        ISagaRepository<ShoppingChore> _repository;
        ISagaRepository<ShoppingChore> _stateMachineRepository;
        Guid _correlationId;

        [TestFixtureSetUp]
        public void Setup()
        {
            _machine = new SuperShopper();
            
            _sessionFactory = TestConfigurator.CreateDocumentStore();
            _repository = new RavenAutomatonymousSagaRepository<ShoppingChore, SuperShopper>(_machine, _sessionFactory);
            _stateMachineRepository = new AutomatonymousStateMachineSagaRepository<ShoppingChore>(_repository,
                x => false, Enumerable.Empty<StateMachineEventCorrelation<ShoppingChore>>());
            _correlationId = NewId.NextGuid();

            _test = TestFactory.ForSaga<ShoppingChore>().New(x =>
            {
                x.UseStateMachineBuilder(_machine);

                x.UseSagaRepository(_stateMachineRepository);

                x.Publish(new GirlfriendYelling
                {
                    CorrelationId = _correlationId
                });

                x.Publish(new GotHitByACar
                {
                    CorrelationId = _correlationId
                });
            });

            _test.Execute();
        }

        [TestFixtureTearDown]
        public void Teardown()
        {
            _test.Dispose();
            _sessionFactory.Dispose();
        }
        
        /// <summary>
        ///     Why to exit the door to go shopping
        /// </summary>
        class GirlfriendYelling :
            CorrelatedBy<Guid>
        {
            public Guid CorrelationId { get; set; }
        }


        class GotHitByACar :
            CorrelatedBy<Guid>
        {
            public Guid CorrelationId { get; set; }
        }


        class ShoppingChore :
            SagaStateMachineInstance
        {
            [Obsolete("for serialization")]
            protected ShoppingChore()
            {
            }

            public ShoppingChore(Guid correlationId)
            {
                CorrelationId = correlationId;
            }

            public State CurrentState { get; set; }
            public CompositeEventStatus Everything { get; set; }
            public bool Screwed { get; set; }

            public Guid CorrelationId { get; set; }

            [JsonIgnore]
            public IServiceBus Bus { get; set; }
        }


        class SuperShopper :
            AutomatonymousStateMachine<ShoppingChore>
        {
            public SuperShopper()
            {
                InstanceState(x => x.CurrentState);

                State(() => OnTheWayToTheStore);

                Event(() => ExitFrontDoor);
                Event(() => GotHitByCar);

                Event(() => EndOfTheWorld, x => x.Everything, ExitFrontDoor, GotHitByCar);

                Initially(
                    When(ExitFrontDoor)
                        .Then(state => Console.Write("Leaving!"))
                        .TransitionTo(OnTheWayToTheStore));

                During(OnTheWayToTheStore,
                    When(GotHitByCar)
                        .Then(state => Console.WriteLine("Ouch!!"))
                        .Finalize());

                DuringAny(
                    When(EndOfTheWorld)
                        .Then(state => Console.WriteLine("Screwed!!"))
                        .Then(state => state.Screwed = true));
            }

            public Event<GirlfriendYelling> ExitFrontDoor { get; private set; }
            public Event<GotHitByACar> GotHitByCar { get; private set; }
            public Event EndOfTheWorld { get; private set; }

            public State OnTheWayToTheStore { get; private set; }
        }
    }


    public static class StateMachineSagaTestingExtensions
    {
        public static void UseStateMachineBuilder<TScenario, TSaga, TStateMachine>(
            this SagaTestInstanceConfigurator<TScenario, TSaga> configurator, TStateMachine stateMachine)
            where TSaga : class, SagaStateMachineInstance
            where TScenario : TestScenario
            where TStateMachine : StateMachine<TSaga>
        {
            configurator.UseBuilder(scenario =>
                                    new StateMachineSagaTestBuilderImpl<TScenario, TSaga, TStateMachine>(scenario,
                                        stateMachine, x => { }));
        }

        public static void UseStateMachineBuilder<TScenario, TSaga, TStateMachine>(
            this SagaTestInstanceConfigurator<TScenario, TSaga> configurator, TStateMachine stateMachine,
            Action<StateMachineSagaRepositoryConfigurator<TSaga>> configureCallback)
            where TSaga : class, SagaStateMachineInstance
            where TScenario : TestScenario
            where TStateMachine : StateMachine<TSaga>
        {
            configurator.UseBuilder(scenario =>
                                    new StateMachineSagaTestBuilderImpl<TScenario, TSaga, TStateMachine>(scenario,
                                        stateMachine, configureCallback));
        }

        public static TSaga ContainsInState<TSaga>(this SagaList<TSaga> sagas, Guid sagaId,
            State state, StateMachine<TSaga> machine)
            where TSaga : class, SagaStateMachineInstance
        {
            bool any = sagas.Any(x => x.CorrelationId == sagaId && machine.InstanceStateAccessor.Get(x).Equals(state));
            return any ? sagas.Contains(sagaId) : null;
        }
    }
}