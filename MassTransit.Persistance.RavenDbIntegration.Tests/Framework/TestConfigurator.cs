using System;
using System.IO;
using Raven.Client.Document;
using Raven.Client.Embedded;

namespace MassTransit.Persistance.RavenDbIntegration.Tests.Framework
{
    public class TestConfigurator
    {
        public static DocumentStore CreateDocumentStore()
        {
            var path = "./db";

            if (Directory.Exists(path)) Directory.Delete(path, true);

            var ds = new EmbeddableDocumentStore{DataDirectory = path};

            return ds;
        } 
    }
}