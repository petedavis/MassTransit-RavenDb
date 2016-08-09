using System;

namespace MassTransit.RavenDbIntegration.Tests.Saga.Messages
{
    public class InitiateSimpleSaga : CorrelatedBy<Guid>
    {
        public InitiateSimpleSaga()
        {
        }

        public InitiateSimpleSaga(Guid correlationId)
        {
            CorrelationId = correlationId;
        }

        public Guid CorrelationId { get; }

        public string Name { get; set; }
    }
}