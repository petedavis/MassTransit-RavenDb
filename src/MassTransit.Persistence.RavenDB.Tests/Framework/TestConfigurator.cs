using Raven.Client.Embedded;

namespace MassTransit.Persistence.RavenDB.Tests.Framework
{
    using Raven.Client;
    using Raven.Client.Document;

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