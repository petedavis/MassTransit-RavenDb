using System;
using MassTransit.RavenDbIntegration.Saga.Context;
using Raven.Client;

namespace MassTransit.RavenDbIntegration.Saga
{
    [Obsolete("Use RavenDbSagaRepository")]
    public class RavenSagaRepository<TSaga> : RavenDbSagaRepository<TSaga>
        where TSaga : class, ISagaDocument
    {

        public RavenSagaRepository(string url, string database)
            : base(url, database)
        {
        }

        public RavenSagaRepository(IDocumentStore documentStore,
            IRavenDbSagaConsumeContextFactory ravenDbSagaConsumeContextFactory)
            : base(documentStore, ravenDbSagaConsumeContextFactory)
        {
        }
    }
}