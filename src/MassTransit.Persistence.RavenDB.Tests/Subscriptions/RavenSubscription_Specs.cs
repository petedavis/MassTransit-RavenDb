// Copyright 2007-2012 Chris Patterson, Dru Sellers, Travis Smith, et. al.
//  
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use
// this file except in compliance with the License. You may obtain a copy of the 
// License at 
// 
//     http://www.apache.org/licenses/LICENSE-2.0 
// 
// Unless required by applicable law or agreed to in writing, software distributed
// under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR 
// CONDITIONS OF ANY KIND, either express or implied. See the License for the 
// specific language governing permissions and limitations under the License.
namespace MassTransit.Persistence.RavenDB.Tests.Subscriptions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using BusConfigurators;
    using Framework;
    using MassTransit.Subscriptions.Coordinator;
    using NUnit.Framework;
    using Raven.Client;
    using Raven.Client.Document;
    using Raven.Client.Indexes;
    using Testing;

    [TestFixture]
    public class When_a_bus_has_peer_subscriptions :
        LoopbackLocalAndRemoteTestFixture
    {
        [Test]
        public void Should_recover_the_subscriptions_after_restarting()
        {
            Assert.IsTrue(LocalBus.HasSubscription<Hello>().Any(), "Initial subscription not found");

            RemoteBus.Dispose();
            RemoteBus = null;

            LocalBus.Dispose();
            LocalBus = null;
            
            // now we need to reload the local bus
            LocalBus = ServiceBusFactory.New(ConfigureLocalBus);
            
            Assert.IsTrue(LocalBus.HasSubscription<Hello>().Any(), "Subscription not found after restart");
        }

        IDocumentStore _documentStore;

        protected override void EstablishContext()
        {
            var documentStore = TestConfigurator.CreateDocumentStore();

            // Create an index where we search based on a bus uri
            documentStore.DatabaseCommands.PutIndex("PersistentSubscriptions/ByBusUri",
                                                    new IndexDefinitionBuilder<PersistentSubscription>
                                                    {
                                                        Map = posts => from post in posts
                                                                       select new { post.BusUri }
                                                    });
            _documentStore = documentStore;

            base.EstablishContext();
        }

        protected override void TeardownContext()
        {
            base.TeardownContext();

            DumpSubscriptionContent();

            if (_documentStore != null)
                _documentStore.Dispose();
        }

        void DumpSubscriptionContent()
        {
            IList<PersistentSubscription> subscriptions;

            using (var session = _documentStore.OpenSession())
            {
                subscriptions = session.Query<PersistentSubscription>()
                    .OrderBy(x => x.PeerUri)
                    .ThenBy(x => x.MessageName)
                    .ThenBy(x => x.EndpointUri)
                    .ToList();
            }

            foreach (PersistentSubscription subscription in subscriptions)
            {
                Console.WriteLine(subscription);
            }
        }


        protected override void ConfigureLocalBus(ServiceBusConfigurator configurator)
        {
            base.ConfigureRemoteBus(configurator);

            configurator.UseRavenDbSubscriptionStorage(_documentStore);

            base.ConfigureLocalBus(configurator);
        }


        protected override void ConfigureRemoteBus(ServiceBusConfigurator configurator)
        {
            base.ConfigureRemoteBus(configurator);

            configurator.UseRavenDbSubscriptionStorage(_documentStore);

            configurator.Subscribe(x => { x.Handler<Hello>(message => { }); });
        }

        interface Hello
        {
        }
    }
}