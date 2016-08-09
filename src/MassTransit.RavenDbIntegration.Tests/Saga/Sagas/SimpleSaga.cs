using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using MassTransit.RavenDbIntegration.Saga;
using MassTransit.RavenDbIntegration.Tests.Saga.Messages;
using MassTransit.Saga;
using Raven.Imports.Newtonsoft.Json;

namespace MassTransit.RavenDbIntegration.Tests.Saga.Sagas
{
    public class SimpleSaga : RavenSagaDocumentBase<SimpleSaga>,
        InitiatedBy<InitiateSimpleSaga>,
        Orchestrates<CompleteSimpleSaga>,
        Observes<ObservableSagaMessage, SimpleSaga>
    {
        public bool Completed { get; set; }

        public bool Initiated { get; set; }

        public bool Observed { get; set; }

        public string Name { get; set; }

        public Task Consume(ConsumeContext<InitiateSimpleSaga> context)
        {
            Initiated = true;
            Name = context.Message.Name;

            return Task.FromResult(0);
        }

        public Task Consume(ConsumeContext<ObservableSagaMessage> message)
        {
            Observed = true;

            return Task.FromResult(0);
        }

        [JsonIgnore]
        public Expression<Func<SimpleSaga, ObservableSagaMessage, bool>> CorrelationExpression
        {
            get { return (saga, message) => saga.Name == message.Name; }
        }

        public Task Consume(ConsumeContext<CompleteSimpleSaga> message)
        {
            Completed = true;

            return Task.FromResult(0);
        }
    }
}