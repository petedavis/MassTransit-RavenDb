using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MassTransit.Context;
using MassTransit.Logging;
using MassTransit.Saga;
using MassTransit.Util;
using Raven.Client;

namespace MassTransit.Persistence.RavenDB.Saga
{

    public class RavenSagaConsumeContext<TSaga, TMessage> :
        ConsumeContextProxyScope<TMessage>,
        SagaConsumeContext<TSaga, TMessage>
        where TMessage : class
        where TSaga : class, ISaga
    {
        static readonly ILog Log = Logger.Get<RavenSagaRepository<TSaga>>();
        readonly IAsyncDocumentSession _dbContext;

        public RavenSagaConsumeContext(IAsyncDocumentSession dbContext, ConsumeContext<TMessage> context, TSaga instance)
            : base(context)
        {
            Saga = instance;
            _dbContext = dbContext;
        }

        Guid? MessageContext.CorrelationId => Saga.CorrelationId;

        SagaConsumeContext<TSaga, T> SagaConsumeContext<TSaga>.PopContext<T>()
        {
            var context = this as SagaConsumeContext<TSaga, T>;
            if (context == null)
                throw new ContextException($"The ConsumeContext<{TypeMetadataCache<TMessage>.ShortName}> could not be cast to {TypeMetadataCache<T>.ShortName}");

            return context;
        }

        public async Task SetCompleted()
        {
            _dbContext.Delete(Saga);

            IsCompleted = true;
            if (Log.IsDebugEnabled)
            {
                Log.DebugFormat("SAGA:{0}:{1} Removed {2}", TypeMetadataCache<TSaga>.ShortName, TypeMetadataCache<TMessage>.ShortName,
                    Saga.CorrelationId);
            }

            await _dbContext.SaveChangesAsync();
        }

        public bool IsCompleted { get; private set; }
        public TSaga Saga { get; }
    }
}
