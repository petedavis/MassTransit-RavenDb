using System;
using System.Threading.Tasks;
using MassTransit.Context;
using MassTransit.Logging;
using MassTransit.Util;
using Raven.Client;

namespace MassTransit.RavenDbIntegration.Saga.Context
{
    public class RavenDbSagaConsumeContext<TSaga, TMessage> :
        ConsumeContextProxyScope<TMessage>,
        SagaConsumeContext<TSaga, TMessage>
        where TMessage : class
        where TSaga : class, ISagaDocument
    {
        static readonly ILog _log = Logger.Get<RavenDbSagaRepository<TSaga>>();
        readonly IAsyncDocumentSession _session;
        readonly bool _existing;

        public RavenDbSagaConsumeContext(IAsyncDocumentSession session, ConsumeContext<TMessage> context, TSaga instance, bool existing = true)
            : base(context)
        {
            Saga = instance;
            _session = session;
            _existing = existing;
        }

        Guid? MessageContext.CorrelationId => Saga.CorrelationId;

        public SagaConsumeContext<TSaga, T> PopContext<T>() where T : class
        {
            var context = this as SagaConsumeContext<TSaga, T>;
            if (context == null)
                throw new ContextException($"The ConsumeContext<{TypeMetadataCache<TMessage>.ShortName}> could not be cast to {TypeMetadataCache<T>.ShortName}");

            return context;
        }

        public async Task SetCompleted()
        {
            IsCompleted = true;

            if (_existing)
            {
                var key = SagaKeyProvider.GetKey<TSaga>(Saga.CorrelationId);
                _session.Delete(key);
                await _session.SaveChangesAsync().ConfigureAwait(false);

                if (_log.IsDebugEnabled)
                    _log.DebugFormat("SAGA:{0}:{1} Removed {2}", TypeMetadataCache<TSaga>.ShortName, TypeMetadataCache<TMessage>.ShortName, Saga.CorrelationId);
            }
        }

        public TSaga Saga { get; }

        public bool IsCompleted { get; private set; }
    }
}