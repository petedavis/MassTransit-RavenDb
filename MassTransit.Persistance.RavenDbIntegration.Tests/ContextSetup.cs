using System.Diagnostics;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using log4net.Config;

namespace MassTransit.Persistance.RavenDbIntegration.Tests
{
    [SetUpFixture]
    public class ContextSetup
    {
        [SetUp]
        public void Before_any()
        {
            Trace.WriteLine("Setting Up Log4net");

            string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            string file = Path.Combine(path, "test.log4net.xml");

            XmlConfigurator.Configure(new FileInfo(file));
        }
    }
}