using System;

namespace MassTransit.RavenDbIntegration.Saga
{
    public abstract class RavenSagaDocumentBase<TSaga> : ISagaDocument where TSaga : RavenSagaDocumentBase<TSaga>
    {
        public string Id => SagaKeyProvider.GetKey<TSaga>(CorrelationId);

        public Guid CorrelationId { get; set; }
    }
}