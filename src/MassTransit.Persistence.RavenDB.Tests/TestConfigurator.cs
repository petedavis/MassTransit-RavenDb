using Raven.Client;
using Raven.Client.Document;
using Raven.Client.Embedded;

namespace MassTransit.Persistence.RavenDB.Tests
{
    public class TestConfigurator
    {
        public static IDocumentStore CreateDocumentStore()
        {
            var documentStore = new EmbeddableDocumentStore
            {
                RunInMemory = true,
                Conventions =
                {
                    DefaultQueryingConsistency = ConsistencyOptions.AlwaysWaitForNonStaleResultsAsOfLastWrite
                }
            };

            documentStore.Initialize();

            return documentStore;
        } 
    }
}