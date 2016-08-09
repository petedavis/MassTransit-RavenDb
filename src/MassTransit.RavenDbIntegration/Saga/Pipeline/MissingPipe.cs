using System.Threading.Tasks;
using MassTransit.Logging;
using MassTransit.Pipeline;
using MassTransit.RavenDbIntegration.Saga.Context;
using MassTransit.Util;
using Raven.Client;

namespace MassTransit.RavenDbIntegration.Saga.Pipeline
{
    public class MissingPipe<TSaga, TMessage> :
        IPipe<SagaConsumeContext<TSaga, TMessage>>
        where TSaga : class, ISagaDocument
        where TMessage : class
    {
        static readonly ILog _log = Logger.Get<RavenDbSagaRepository<TSaga>>();
        readonly IAsyncDocumentSession _session;
        readonly IRavenDbSagaConsumeContextFactory _ravenDbSagaConsumeContextFactory;
        readonly IPipe<SagaConsumeContext<TSaga, TMessage>> _next;

        public MissingPipe(IAsyncDocumentSession session, IPipe<SagaConsumeContext<TSaga, TMessage>> next,
            IRavenDbSagaConsumeContextFactory ravenDbSagaConsumeContextFactory)
        {
            _session = session;
            _next = next;
            _ravenDbSagaConsumeContextFactory = ravenDbSagaConsumeContextFactory;
        }

        public void Probe(ProbeContext context)
        {
            _next.Probe(context);
        }

        public async Task Send(SagaConsumeContext<TSaga, TMessage> context)
        {
            if (_log.IsDebugEnabled)
                _log.DebugFormat("SAGA:{0}:{1} Added {2}", TypeMetadataCache<TSaga>.ShortName, context.Saga.CorrelationId,
                    TypeMetadataCache<TMessage>.ShortName);

            SagaConsumeContext<TSaga, TMessage> proxy = _ravenDbSagaConsumeContextFactory.Create(_session, context, context.Saga, false);

            await _next.Send(proxy).ConfigureAwait(false);

            if (!proxy.IsCompleted)
            {
                await _session.StoreAsync(context.Saga).ConfigureAwait(false);
            }
        }
    }
}