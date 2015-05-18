using System;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;
using MassTransit.Exceptions;
using MassTransit.Logging;
using MassTransit.Pipeline;
using MassTransit.Saga;
using MassTransit.Util;
using Raven.Client;
using Raven.Client.Linq;

namespace MassTransit.Persistance.RavenDb
{
    public class RavenDbSagaRepository<TSaga> : 
        ISagaRepository<TSaga> where TSaga : class, ISaga
    {
        private static readonly ILog Log = Logger.Get<RavenDbSagaRepository<TSaga>>();

        private readonly IDocumentStore _documentStore;


        public RavenDbSagaRepository(IDocumentStore documentStore)
        {
            _documentStore = documentStore;
        }

        public IEnumerable<Action<IConsumeContext<TMessage>>> GetSaga<TMessage>(IConsumeContext<TMessage> context, Guid sagaId, InstanceHandlerSelector<TSaga, TMessage> selector, ISagaPolicy<TSaga, TMessage> policy) where TMessage : class
        {
            using(var session = _documentStore.OpenSession())
            {
                var instance = session.Load<TSaga>(GetDocumentId(sagaId));
                if(instance == null)
                {
                    if(policy.CanCreateInstance(context))
                    {
                        yield return x =>
                            {
                                if (Log.IsDebugEnabled)
                                    Log.DebugFormat("SAGA: {0} Creating New {1} for {2}",
                                                     typeof (TSaga).ToFriendlyName(), sagaId,
                                                     typeof (TMessage).ToFriendlyName());

                                try
                                {
                                    instance = policy.CreateInstance(x, sagaId);

                                    foreach (var callback in selector(instance, x))
                                    {
                                        callback(x);
                                    }

                                    if (!policy.CanRemoveInstance(instance))
                                        session.Store(instance, GetDocumentId(sagaId));
                                }
                                catch (Exception ex)
                                {
                                    var sex = new SagaException("Create Saga Instance Exception", typeof(TSaga), typeof(TMessage), sagaId, ex);
                                    if (Log.IsErrorEnabled)
                                        Log.Error(sex);
                                    
                                    throw sex;
                                }
                            };
                    }
                    else
                    {
                        if (Log.IsDebugEnabled)
                            Log.DebugFormat("SAGA: {0} Ignoring Missing {1} for {2}", typeof(TSaga).ToFriendlyName(), sagaId,
                                typeof(TMessage).ToFriendlyName());
                    }
                }
                else
                {
                    if (policy.CanUseExistingInstance(context))
                    {
                        yield return x =>
                        {
                            if (Log.IsDebugEnabled)
                                Log.DebugFormat("SAGA: {0} Using Existing {1} for {2}", typeof(TSaga).ToFriendlyName(), sagaId,
                                    typeof(TMessage).ToFriendlyName());

                            try
                            {
                                foreach (var callback in selector(instance, x))
                                {
                                    callback(x);
                                }

                                if (policy.CanRemoveInstance(instance))
                                    session.Delete(instance);
                            }
                            catch (Exception ex)
                            {
                                var sex = new SagaException("Existing Saga Instance Exception", typeof(TSaga), typeof(TMessage), sagaId, ex);
                                if (Log.IsErrorEnabled)
                                    Log.Error(sex);
                                
                                throw sex;
                            }
                        };
                    }
                    else
                    {
                        if (Log.IsDebugEnabled)
                            Log.DebugFormat("SAGA: {0} Ignoring Existing {1} for {2}", typeof(TSaga).ToFriendlyName(), sagaId,
                                typeof(TMessage).ToFriendlyName());
                    }
                }

                session.SaveChanges();
            }
        }

        private string GetDocumentId(Guid sagaId)
        {
            return typeof(TSaga).Name + "/" + sagaId;
        }

        public IEnumerable<Guid> Find(ISagaFilter<TSaga> filter)
        {
            return Where(filter, x => x.CorrelationId);
        }

        public IEnumerable<TSaga> Where(ISagaFilter<TSaga> filter)
        {
            using (var session = _documentStore.OpenSession())
            {
                List<TSaga> result = session.Query<TSaga>()
                    .Where(filter.FilterExpression)
                    .ToList();

                return result;
            }
        }

        public IEnumerable<TResult> Where<TResult>(ISagaFilter<TSaga> filter, Func<TSaga, TResult> transformer)
        {
            return Where(filter).Select(transformer);
        }

        public IEnumerable<TResult> Select<TResult>(Func<TSaga, TResult> transformer)
        {
            using (var session = _documentStore.OpenSession())
            {
                List<TResult> result = session.Query<TSaga>()
                    .Select(transformer)
                    .ToList();

                return result;
            }
        }
    }
}
