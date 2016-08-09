using System;

namespace MassTransit.RavenDbIntegration.Tests.Saga.Messages
{
    public class CompleteSimpleSaga :
        SimpleSagaMessageBase
    {
        public CompleteSimpleSaga(Guid correlationId)
            : base(correlationId)
        {
        }
    }
}