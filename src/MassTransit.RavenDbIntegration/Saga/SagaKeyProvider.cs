using System;

namespace MassTransit.RavenDbIntegration.Saga
{
    public static class SagaKeyProvider
    {
        public static string GetKey<TSaga>(Guid correlationId)
        {
            return $"{typeof(TSaga).Name.ToLowerInvariant()}s/{correlationId}";
        }
    }
}
