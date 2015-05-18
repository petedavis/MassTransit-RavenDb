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

using MassTransit.Persistence.RavenDB.Tests.Framework;

namespace MassTransit.Persistence.RavenDB.Tests.Sagas
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using Magnum.TestFramework;
    using NUnit.Framework;
    using Raven.Abstractions.Data;
    using Raven.Client;
    using Raven.Client.Document;
    using Raven.Client.Indexes;

    [TestFixture, Category("Integration")]
    public class Saving_correlation_id_values_to_ravendb_document_store

    {
        [SetUp]
        public void Setup()
        {
            _documentStore = TestConfigurator.CreateDocumentStore();
        }

        [TearDown]
        public void Teardown()
        {
            if (_documentStore != null)
                _documentStore.Dispose();
        }

        [Test, Integration]
        public void Should_store_them_in_order()
        {
            var ids = new List<Guid>(Enumerable.Repeat(1, 100).Select(x =>
                {
                    Thread.Sleep(10);
                    return NewId.NextGuid();
                }));

            using (var session = _documentStore.OpenSession())
            {
                foreach (Guid id in ids)
                    session.Store(new ConcurrentSaga(id), "ConcurrentSaga/" + id);

                session.SaveChanges();
            }

            using (var session = _documentStore.OpenSession())
            {
                IList<ConcurrentSaga> results = session.Query<ConcurrentSaga>().ToList();
                
                Assert.IsTrue(ids.SequenceEqual(results.Select(x => x.CorrelationId)));
            }
        }

        IDocumentStore _documentStore;
    }
}