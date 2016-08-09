using Raven.Client;

namespace MassTransit.RavenDbIntegration.Saga.Context
{
    public class RavenDbSagaConsumeContextFactory
        : IRavenDbSagaConsumeContextFactory
    {
        public SagaConsumeContext<TSaga, TMessage> Create<TSaga, TMessage>(IAsyncDocumentSession documentStore, ConsumeContext<TMessage> message, TSaga instance, bool existing = true) where TSaga : class, ISagaDocument where TMessage : class
        {
            return new RavenDbSagaConsumeContext<TSaga, TMessage>(documentStore, message, instance, existing);
        }
    }
}