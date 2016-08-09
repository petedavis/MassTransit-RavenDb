using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MassTransit.Logging;
using MassTransit.Pipeline;
using MassTransit.RavenDbIntegration.Saga.Context;
using MassTransit.RavenDbIntegration.Saga.Pipeline;
using MassTransit.Saga;
using MassTransit.Util;
using Raven.Client;
using Raven.Client.Document;

namespace MassTransit.RavenDbIntegration.Saga
{
    public class RavenDbSagaRepository<TSaga> :
        ISagaRepository<TSaga>
        where TSaga : class, ISagaDocument
    {
        static readonly ILog _log = Logger.Get<RavenDbSagaRepository<TSaga>>();
        private readonly IDocumentStore _documentStore;
        private readonly IRavenDbSagaConsumeContextFactory _ravenDbSagaConsumeContextFactory;

        public RavenDbSagaRepository(string url, string database)
            : this(new DocumentStore {Url = url, DefaultDatabase = database}.Initialize(), new RavenDbSagaConsumeContextFactory())
        {
        }

        public RavenDbSagaRepository(IDocumentStore documentStore, IRavenDbSagaConsumeContextFactory ravenDbSagaConsumeContextFactory)
        {
            _documentStore = documentStore;
            _ravenDbSagaConsumeContextFactory = ravenDbSagaConsumeContextFactory;
        }

        public void Probe(ProbeContext context)
        {
            var scope = context.CreateScope("sagaRepository");

            scope.Set(new
            {
                Persistence = "ravendb",
                SagaType = TypeMetadataCache<TSaga>.ShortName,
                Properties = TypeMetadataCache<TSaga>.ReadWritePropertyCache.Select(x => x.Property.Name).ToArray()
            });
        }

        public async Task Send<T>(ConsumeContext<T> context, ISagaPolicy<TSaga, T> policy,
            IPipe<SagaConsumeContext<TSaga, T>> next) where T : class
        {
            if (!context.CorrelationId.HasValue)
                throw new SagaException("The CorrelationId was not specified", typeof(TSaga), typeof(T));

            using (var session = _documentStore.OpenAsyncSession())
            {
                session.Advanced.UseOptimisticConcurrency = true;

                TSaga instance;

                if (policy.PreInsertInstance(context, out instance))
                {
                    await PreInsertSagaInstance(session, context, instance).ConfigureAwait(false);
                }

                if (instance == null)
                {
                    instance = await session.LoadAsync<TSaga>(SagaKeyProvider.GetKey<TSaga>(context.CorrelationId.Value)).ConfigureAwait(false);
                }

                if (instance == null)
                {
                    var missingSagaPipe = new MissingPipe<TSaga, T>(session, next, _ravenDbSagaConsumeContextFactory);

                    await policy.Missing(context, missingSagaPipe).ConfigureAwait(false);
                }
                else
                {
                    await SendToInstance(session, context, policy, next, instance).ConfigureAwait(false);
                }

                await session.SaveChangesAsync(context.CancellationToken).ConfigureAwait(false);
            }
        }

        public async Task SendQuery<T>(SagaQueryConsumeContext<TSaga, T> context, ISagaPolicy<TSaga, T> policy, IPipe<SagaConsumeContext<TSaga, T>> next)
            where T : class
        {
            try
            {
                using (var session = _documentStore.OpenAsyncSession())
                {
                    session.Advanced.UseOptimisticConcurrency = true;

                    IList<TSaga> sagaInstances =
                        await session.Query<TSaga>().Where(context.Query.FilterExpression).ToListAsync().ConfigureAwait(false);

                    if (!sagaInstances.Any())
                    {
                        var missingPipe = new MissingPipe<TSaga, T>(session, next, _ravenDbSagaConsumeContextFactory);

                        await policy.Missing(context, missingPipe).ConfigureAwait(false);
                    }
                    else
                    {
                        foreach (var instance in sagaInstances)
                        {
                            await SendToInstance(session, context, policy, next, instance).ConfigureAwait(false);
                        }
                    }

                    await session.SaveChangesAsync(context.CancellationToken).ConfigureAwait(false);
                }
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

        async Task PreInsertSagaInstance<T>(IAsyncDocumentSession session, ConsumeContext<T> context, TSaga instance) where T : class
        {
            try
            {
                await session.StoreAsync(instance, context.CancellationToken).ConfigureAwait(false);

                _log.DebugFormat("SAGA:{0}:{1} Insert {2}", TypeMetadataCache<TSaga>.ShortName, instance.CorrelationId, TypeMetadataCache<T>.ShortName);
            }
            catch (Exception ex)
            {
                if (_log.IsDebugEnabled)
                    _log.DebugFormat("SAGA:{0}:{1} Dupe {2} - {3}", TypeMetadataCache<TSaga>.ShortName, instance.CorrelationId, TypeMetadataCache<T>.ShortName,
                        ex.Message);
            }
        }

        async Task SendToInstance<T>(IAsyncDocumentSession session, ConsumeContext<T> context, ISagaPolicy<TSaga, T> policy, IPipe<SagaConsumeContext<TSaga, T>> next, TSaga instance)
            where T : class
        {
            try
            {
                if (_log.IsDebugEnabled)
                    _log.DebugFormat("SAGA:{0}:{1} Used {2}", TypeMetadataCache<TSaga>.ShortName, instance.CorrelationId, TypeMetadataCache<T>.ShortName);

                SagaConsumeContext<TSaga, T> sagaConsumeContext = _ravenDbSagaConsumeContextFactory.Create(session, context, instance);

                await policy.Existing(sagaConsumeContext, next).ConfigureAwait(false);

                if (!sagaConsumeContext.IsCompleted)
                    await UpdateRavenDbSaga(session, context, instance).ConfigureAwait(false);
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

        Task UpdateRavenDbSaga(IAsyncDocumentSession session, PipeContext context, TSaga instance)
        {
            return session.StoreAsync(instance, context.CancellationToken);
        }
    }
}