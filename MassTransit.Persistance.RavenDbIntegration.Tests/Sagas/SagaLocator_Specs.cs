using System.Data;
using MassTransit.Context;
using MassTransit.Persistance.RavenDb;
using MassTransit.Persistance.RavenDbIntegration.Tests.Framework;
using Raven.Client.Document;

namespace MassTransit.Persistance.RavenDbIntegration.Tests.Sagas
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Magnum.Extensions;
    using MassTransit.Saga;
    using NUnit.Framework;
    using Saga;

    [TestFixture, Category("Integration")]
    public class When_using_the_saga_locator_with_NHibernate
    {
        private DocumentStore _store;
        Guid _sagaId;

        [SetUp]
        public void Setup()
        {
            _store = TestConfigurator.CreateDocumentStore();

            _sagaId = NewId.NextGuid();
        }

        [TearDown]
        public void teardown()
        {
            _store.Dispose();
        }

        IEnumerable<Action<IConsumeContext<InitiateSimpleSaga>>> GetHandlers(TestSaga instance,
                                                                             IConsumeContext<InitiateSimpleSaga> context)
        {
            yield return x => instance.RaiseEvent(TestSaga.Initiate, x.Message);
        }

        [Test]
        public void A_correlated_message_should_find_the_correct_saga()
        {
            var repository = new RavenDbSagaRepository<TestSaga>(_store);
            var ping = new PingMessage(_sagaId);

            var initiatePolicy = new InitiatingSagaPolicy<TestSaga, InitiateSimpleSaga>(x => x.CorrelationId, x => false);

            var message = new InitiateSimpleSaga(_sagaId);
            IConsumeContext<InitiateSimpleSaga> context = new ConsumeContext<InitiateSimpleSaga>(ReceiveContext.Empty(), message); ;

            repository.GetSaga(context, message.CorrelationId, GetHandlers, initiatePolicy)
                .Each(x => x(context));

            List<TestSaga> sagas = repository.Where(x => x.CorrelationId == _sagaId).ToList();

            Assert.AreEqual(1, sagas.Count);
            Assert.IsNotNull(sagas[0]);
            Assert.AreEqual(_sagaId, sagas[0].CorrelationId);
        }
    }
}