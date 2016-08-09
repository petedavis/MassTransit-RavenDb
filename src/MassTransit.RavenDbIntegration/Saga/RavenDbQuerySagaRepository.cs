using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MassTransit.Saga;
using Raven.Client;
using Raven.Client.Document;

namespace MassTransit.RavenDbIntegration.Saga
{
    public class RavenDbQuerySagaRepository<TSaga> :
        IQuerySagaRepository<TSaga>
        where TSaga : class, ISaga
    {
        readonly IDocumentStore _documentStore;

        public RavenDbQuerySagaRepository(string url, string database)
            : this(new DocumentStore {Url = url, DefaultDatabase = database}.Initialize())
        {
        }

        public RavenDbQuerySagaRepository(IDocumentStore documentStore)
        {
            _documentStore = documentStore;
        }

        public async Task<IEnumerable<Guid>> Find(ISagaQuery<TSaga> query)
        {
            using (var session = _documentStore.OpenAsyncSession())
            {
                return await session.Query<TSaga>()
                    .Where(query.FilterExpression)
                    .Select(x => x.CorrelationId)
                    .ToListAsync()
                    .ConfigureAwait(false);
            }
        }
    }
}
