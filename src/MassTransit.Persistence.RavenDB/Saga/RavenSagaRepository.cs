using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MassTransit.Logging;
using MassTransit.Pipeline;
using MassTransit.Saga;
using MassTransit.Util;
using Raven.Client;
using Raven.Client.Linq;

namespace MassTransit.Persistence.RavenDB.Saga
{
    public class RavenSagaRepository<TSaga> :
        ISagaRepository<TSaga>,
        IQuerySagaRepository<TSaga>
        where TSaga : class, ISaga
    {
        static readonly ILog _log = Logger.Get<RavenSagaRepository<TSaga>>();

        private readonly IDocumentStore _documentStore;


        public RavenSagaRepository(IDocumentStore documentStore)
        {
            _documentStore = documentStore;
        }

        public async Task<IEnumerable<Guid>> Find(ISagaQuery<TSaga> query)
        {
            using (var session = _documentStore.OpenAsyncSession())
            {
                var guids = await session.Query<TSaga>()
                    .Where(query.FilterExpression)
                    .Select(x => x.CorrelationId)
                    .ToListAsync();
                return guids;
            }
        }

        public void Probe(ProbeContext context)
        {
            // Taken from:
            // http://stackoverflow.com/questions/14296266/how-can-i-get-all-entity-names-collection-names-from-a-ravendb-database
            ProbeContext scope = context.CreateScope("sagaRepository");

            IEnumerable<string> results = _documentStore.DatabaseCommands
                       .GetTerms("Raven/DocumentsByEntityName", "Tag", "", 1024);

            scope.Set(new
            {
                Persistence = "ravenDB",
                Entities = results
            });
        }

        public async Task Send<T>(ConsumeContext<T> context, ISagaPolicy<TSaga, T> policy, IPipe<SagaConsumeContext<TSaga, T>> next) where T : class
        {
            if (!context.CorrelationId.HasValue)
                throw new SagaException("The CorrelationId was not specified", typeof(TSaga), typeof(T));

            Guid sagaId = context.CorrelationId.Value;

            using (var session = _documentStore.OpenAsyncSession())
            {
                TSaga instance;
                if (policy.PreInsertInstance(context, out instance))
                    await PreInsertSagaInstance<T>(session, instance);

                try
                {
                    if (instance == null)
                        instance = await session.Query<TSaga>().FirstOrDefaultAsync(x => x.CorrelationId == sagaId);
                    if (instance == null)
                    {
                        var missingSagaPipe = new MissingPipe<T>(session, next);

                        await policy.Missing(context, missingSagaPipe);
                    }
                    else
                    {
                        if (_log.IsDebugEnabled)
                            _log.DebugFormat("SAGA:{0}:{1} Used {2}", TypeMetadataCache<TSaga>.ShortName, instance.CorrelationId, TypeMetadataCache<T>.ShortName);

                        var sagaConsumeContext = new RavenSagaConsumeContext<TSaga, T>(session, context, instance);

                        await policy.Existing(sagaConsumeContext, next);
                    }

                    await session.SaveChangesAsync();
                }
                catch (Exception e)
                {
                    _log.Error(string.Format("SAGA:{0}:{1} {2}", TypeMetadataCache<TSaga>.ShortName, context.CorrelationId, e.Message), e);
                    throw;
                }
            }
        }

        static async Task<bool> PreInsertSagaInstance<T>(IAsyncDocumentSession dbContext, TSaga instance)
        {
            try
            {
                await dbContext.StoreAsync(instance);
                await dbContext.SaveChangesAsync();

                _log.DebugFormat("SAGA:{0}:{1} Insert {2}", TypeMetadataCache<TSaga>.ShortName, instance.CorrelationId,
                    TypeMetadataCache<T>.ShortName);

                return true;
            }
            catch (Exception ex)
            {
                if (_log.IsDebugEnabled)
                {
                    _log.DebugFormat("SAGA:{0}:{1} Dupe {2} - {3}", TypeMetadataCache<TSaga>.ShortName, instance.CorrelationId,
                        TypeMetadataCache<T>.ShortName, ex.Message);
                }
            }

            return false;
        }

        public async Task SendQuery<T>(SagaQueryConsumeContext<TSaga, T> context, ISagaPolicy<TSaga, T> policy, IPipe<SagaConsumeContext<TSaga, T>> next) where T : class
        {
            using (var session = _documentStore.OpenAsyncSession())
            {
                try
                {
                    var sagaInstances = await session.Query<TSaga>().Where(context.Query.FilterExpression).ToListAsync();
                    if (sagaInstances.Count == 0)
                    {
                        var missingSagaPipe = new MissingPipe<T>(session, next);

                        await policy.Missing(context, missingSagaPipe);
                    }
                    else
                    {
                        foreach (var instance in sagaInstances)
                        {
                            await SendToInstance(context, session, policy, instance, next);
                        }
                    }

                    await session.SaveChangesAsync();
                }
                catch (SagaException sex)
                {
                    if (_log.IsErrorEnabled)
                        _log.Error("Saga Exception Occurred", sex);
                }
                catch (Exception ex)
                {
                    if (_log.IsErrorEnabled)
                        _log.Error($"SAGA:{TypeMetadataCache<TSaga>.ShortName} Exception {TypeMetadataCache<T>.ShortName}", ex);

                    throw new SagaException(ex.Message, typeof(TSaga), typeof(T), Guid.Empty, ex);
                }
            }
        }

        async Task SendToInstance<T>(SagaQueryConsumeContext<TSaga, T> context, IAsyncDocumentSession dbContext, ISagaPolicy<TSaga, T> policy, TSaga instance,
            IPipe<SagaConsumeContext<TSaga, T>> next)
            where T : class
        {
            try
            {
                if (_log.IsDebugEnabled)
                    _log.DebugFormat("SAGA:{0}:{1} Used {2}", TypeMetadataCache<TSaga>.ShortName, instance.CorrelationId, TypeMetadataCache<T>.ShortName);

                var sagaConsumeContext = new RavenSagaConsumeContext<TSaga, T>(dbContext, context, instance);

                await policy.Existing(sagaConsumeContext, next);
            }
            catch (SagaException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new SagaException(ex.Message, typeof(TSaga), typeof(T), instance.CorrelationId, ex);
            }
        }



        /// <summary>
        /// Once the message pipe has processed the saga instance, add it to the saga repository
        /// </summary>
        /// <typeparam name="TMessage"></typeparam>
        class MissingPipe<TMessage> :
            IPipe<SagaConsumeContext<TSaga, TMessage>>
            where TMessage : class
        {
            readonly IPipe<SagaConsumeContext<TSaga, TMessage>> _next;
            readonly IAsyncDocumentSession _session;

            public MissingPipe(IAsyncDocumentSession session, IPipe<SagaConsumeContext<TSaga, TMessage>> next)
            {
                _session = session;
                _next = next;
            }

            void IProbeSite.Probe(ProbeContext context)
            {
                _next.Probe(context);
            }

            public async Task Send(SagaConsumeContext<TSaga, TMessage> context)
            {
                if (_log.IsDebugEnabled)
                {
                    _log.DebugFormat("SAGA:{0}:{1} Added {2}", TypeMetadataCache<TSaga>.ShortName, context.Saga.CorrelationId,
                        TypeMetadataCache<TMessage>.ShortName);
                }

                SagaConsumeContext<TSaga, TMessage> proxy = new RavenSagaConsumeContext<TSaga, TMessage>(_session, context, context.Saga);

                await _next.Send(proxy);

                if (!proxy.IsCompleted)
                    await _session.StoreAsync(context.Saga);

                await _session.SaveChangesAsync();
            }
        }
    }
}
