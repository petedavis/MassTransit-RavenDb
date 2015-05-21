using System;
using System.Collections.Generic;
using System.Linq;
using Automatonymous;
using MassTransit.Exceptions;
using MassTransit.Logging;
using MassTransit.Pipeline;
using MassTransit.Saga;
using MassTransit.Util;
using Raven.Client;
using Raven.Client.Linq;

namespace MassTransit.Persistence.RavenDB
{
    public class RavenAutomatonymousSagaRepository<TInstance, TSaga> : ISagaRepository<TInstance>
        where TInstance : class, ISaga
        where TSaga : class, StateMachine<TInstance>
    {
        private static readonly ILog Log = Logger.Get<RavenAutomatonymousSagaRepository<TInstance, TSaga>>();

        private readonly IDocumentStore _documentStore;


        public RavenAutomatonymousSagaRepository(TSaga saga, IDocumentStore documentStore)
        {
            _documentStore = documentStore;
            documentStore.Conventions.CustomizeJsonSerializer =
                serializer => serializer.Converters.Add(new AutomatonymousStateJsonConverter<TSaga>(saga));
        }

        public IEnumerable<Action<IConsumeContext<TMessage>>> GetSaga<TMessage>(IConsumeContext<TMessage> context, Guid sagaId, InstanceHandlerSelector<TInstance, TMessage> selector, ISagaPolicy<TInstance, TMessage> policy) where TMessage : class
        {
            using(var session = _documentStore.OpenSession())
            {
                var instance = session.Load<TInstance>(GetDocumentId(sagaId));
                if(instance == null)
                {
                    if(policy.CanCreateInstance(context))
                    {
                        yield return x =>
                            {
                                if (Log.IsDebugEnabled)
                                    Log.DebugFormat("SAGA: {0} Creating New {1} for {2}",
                                                     typeof (TInstance).ToFriendlyName(), sagaId,
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
                                    var sex = new SagaException("Create Saga Instance Exception", typeof(TInstance), typeof(TMessage), sagaId, ex);
                                    if (Log.IsErrorEnabled)
                                        Log.Error(sex);
                                    
                                    throw sex;
                                }
                            };
                    }
                    else
                    {
                        if (Log.IsDebugEnabled)
                            Log.DebugFormat("SAGA: {0} Ignoring Missing {1} for {2}", typeof(TInstance).ToFriendlyName(), sagaId,
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
                                Log.DebugFormat("SAGA: {0} Using Existing {1} for {2}", typeof(TInstance).ToFriendlyName(), sagaId,
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
                                var sex = new SagaException("Existing Saga Instance Exception", typeof(TInstance), typeof(TMessage), sagaId, ex);
                                if (Log.IsErrorEnabled)
                                    Log.Error(sex);
                                
                                throw sex;
                            }
                        };
                    }
                    else
                    {
                        if (Log.IsDebugEnabled)
                            Log.DebugFormat("SAGA: {0} Ignoring Existing {1} for {2}", typeof(TInstance).ToFriendlyName(), sagaId,
                                typeof(TMessage).ToFriendlyName());
                    }
                }

                session.SaveChanges();
            }
        }

        private string GetDocumentId(Guid sagaId)
        {
            return typeof(TInstance).Name + "/" + sagaId;
        }

        public IEnumerable<Guid> Find(ISagaFilter<TInstance> filter)
        {
            return Where(filter, x => x.CorrelationId);
        }

        public IEnumerable<TInstance> Where(ISagaFilter<TInstance> filter)
        {
            using (var session = _documentStore.OpenSession())
            {
                List<TInstance> result = session.Query<TInstance>()
                    .Where(filter.FilterExpression)
                    .ToList();

                return result;
            }
        }

        public IEnumerable<TResult> Where<TResult>(ISagaFilter<TInstance> filter, Func<TInstance, TResult> transformer)
        {
            return Where(filter).Select(transformer);
        }

        public IEnumerable<TResult> Select<TResult>(Func<TInstance, TResult> transformer)
        {
            using (var session = _documentStore.OpenSession())
            {
                List<TResult> result = session.Query<TInstance>()
                    .Select(transformer)
                    .ToList();

                return result;
            }
        }
    }
}
