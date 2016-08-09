using Raven.Client;

namespace MassTransit.RavenDbIntegration.Saga.Context
{
    public interface IRavenDbSagaConsumeContextFactory
    {
        SagaConsumeContext<TSaga, TMessage> Create<TSaga, TMessage>(IAsyncDocumentSession documentStore, ConsumeContext<TMessage> message, TSaga instance, bool existing = true)
            where TSaga : class, ISagaDocument
            where TMessage : class;
    }
}