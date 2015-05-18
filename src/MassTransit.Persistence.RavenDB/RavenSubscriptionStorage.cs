

namespace MassTransit.Persistence.RavenDB
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using MassTransit.Logging;
    using MassTransit.Subscriptions.Coordinator;
    using Raven.Client;
    using Raven.Client.Linq;

    public class RavenSubscriptionStorage :
        SubscriptionStorage
    {
        static readonly ILog Log = Logger.Get<RavenSubscriptionStorage>();

        private readonly IDocumentStore _documentStore;

        public RavenSubscriptionStorage(IDocumentStore documentStore)
        {
            _documentStore = documentStore;
        }

        public void Dispose()
        {
        }

        public void Add(PersistentSubscription subscription)
        {
            using(var session = _documentStore.OpenSession())
            {
                var existingSubscription = session.Query<PersistentSubscription>()
                    .Where(x => x.BusUri == subscription.BusUri)
                    .Where(x => x.PeerId == subscription.PeerId)
                    .Where(x => x.SubscriptionId == subscription.SubscriptionId)
                    .SingleOrDefault();

                if (existingSubscription != null)
                {
                    Log.DebugFormat("Updating: {0}", existingSubscription);

                    existingSubscription.Updated = DateTime.UtcNow;
                }
                else
                {
                    Log.DebugFormat("Adding: {0}", subscription);

                    session.Store(subscription, "PersistentSubscription/" + NewId.NextGuid());
                }
                session.SaveChanges();
            }
        }

        public void Remove(PersistentSubscription subscription)
        {
            using (var session = _documentStore.OpenSession())
            {
                var existingSubscription = session.Query<PersistentSubscription>()
                    .Where(x => x.BusUri == subscription.BusUri)
                    .Where(x => x.PeerId == subscription.PeerId)
                    .Where(x => x.SubscriptionId == subscription.SubscriptionId).ToList();

                foreach (var existing in existingSubscription)
                {
                    Log.DebugFormat("Removing: {0}", existing);

                    session.Delete(existing);
                }

                session.SaveChanges();
            }
        }

        public IEnumerable<PersistentSubscription> Load(Uri busUri)
        {
            using (var session = _documentStore.OpenSession())
            {
                var existingSubscription = session.Query<PersistentSubscription>()
                    .Where(x => x.BusUri == busUri)
                    .OrderBy(x => x.PeerId)
                    .ToList();
                
                return existingSubscription;
            }
        }
    }
}
