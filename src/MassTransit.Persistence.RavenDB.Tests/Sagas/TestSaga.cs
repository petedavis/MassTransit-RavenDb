using System;
using System.Runtime.Serialization;
using Magnum.StateMachine;
using MassTransit.Saga;

namespace MassTransit.Persistence.RavenDB.Tests.Sagas
{
    public class TestSaga :
        SagaStateMachine<TestSaga>,
        ISaga
    {
        static TestSaga()
        {
            Define(() =>
                {
                    Correlate(Observation).By((saga, message) => saga.Name == message.Name);

                    Initially(
                        When(Initiate)
                            .Then((saga, message) =>
                                {
                                    saga.WasInitiated = true;
                                    saga.Name = message.Name;
                                })
                            .TransitionTo(Initiated));

                    During(Initiated,
                           When(Observation)
                               .Then((saga, message) => { saga.WasObserved = true; }),
                           When(Complete)
                               .Then((saga, message) => { saga.WasCompleted = true; })
                               .TransitionTo(Completed));
                });
        }

        protected TestSaga()
        {
        }

        public TestSaga(Guid correlationId)
        {
            CorrelationId = correlationId;
        }

        public TestSaga(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            CorrelationId =new Guid(info.GetString("CorrelationId"));
            Name = info.GetString("Name");
            WasInitiated = info.GetBoolean("WasInitiated");
            WasObserved = info.GetBoolean("WasObserved");
            WasCompleted = info.GetBoolean("WasCompleted");
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("CorrelationId", CorrelationId.ToString());
            info.AddValue("Name", Name);
            info.AddValue("WasInitiated", WasInitiated);
            info.AddValue("WasObserved", WasObserved);
            info.AddValue("WasCompleted", WasCompleted);
            base.GetObjectData(info, context);
        }

        public static State Initial { get; set; }
        public static State Completed { get; set; }
        public static State Initiated { get; set; }

        public static Event<InitiateSimpleSaga> Initiate { get; set; }
        public static Event<ObservableSagaMessage> Observation { get; set; }
        public static Event<CompleteSimpleSaga> Complete { get; set; }

        public bool WasInitiated { get; set; }
        public bool WasObserved { get; set; }
        public bool WasCompleted { get; set; }

        public string Name { get; set; }
        public Guid CorrelationId { get; private set; }

        public IServiceBus Bus { get; set; }
    }


    public class SimpleSagaMessageBase :
        CorrelatedBy<Guid>
    {
        public SimpleSagaMessageBase()
        {
        }

        public SimpleSagaMessageBase(Guid correlationId)
        {
            CorrelationId = correlationId;
        }

        public Guid CorrelationId { get; set; }
    }

    [Serializable]
    public class InitiateSimpleSaga :
        SimpleSagaMessageBase
    {
        public InitiateSimpleSaga()
        {
        }

        public InitiateSimpleSaga(Guid correlationId)
            : base(correlationId)
        {
        }

        public string Name { get; set; }
    }


    [Serializable]
    public class ObservableSagaMessage
    {
        public string Name { get; set; }
    }


    [Serializable]
    public class CompleteSimpleSaga :
        SimpleSagaMessageBase
    {
        public CompleteSimpleSaga()
        {
        }

        public CompleteSimpleSaga(Guid correlationId)
            :
                base(correlationId)
        {
        }
    }
}