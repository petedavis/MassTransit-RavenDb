using MassTransit.Log4NetIntegration.Logging;
using NUnit.Framework;

namespace MassTransit.Persistence.RavenDB.Tests
{
    [SetUpFixture]
    public class ContextSetup
    {
        [SetUp]
        public void Before_any()
        {
            Log4NetLogger.Use("test.log4net.xml");
        }
    }
}