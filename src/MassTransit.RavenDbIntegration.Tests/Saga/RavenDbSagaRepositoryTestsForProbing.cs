using MassTransit.RavenDbIntegration.Saga;
using MassTransit.RavenDbIntegration.Tests.Saga.Sagas;
using Moq;
using NUnit.Framework;
using Raven.Tests.Helpers;

namespace MassTransit.RavenDbIntegration.Tests.Saga
{

    [TestFixture]
    public class RavenDbSagaRepositoryTestsForProbing : RavenTestBase
    {
        [Test]
        public void ThenScopeIsReturned()
        {
            _probeContext.Verify(m => m.CreateScope("sagaRepository"));
        }

        [Test]
        public void ThenScopeIsSet()
        {
            _scope.Verify(x => x.Set(It.IsAny<object>()));
        }

        Mock<ProbeContext> _probeContext;
        Mock<ProbeContext> _scope;

        [OneTimeSetUp]
        public void GivenARavenDbSagaRepository_WhenProbing()
        {
            using (var store = NewDocumentStore())
            {
                _scope = new Mock<ProbeContext>();

                _probeContext = new Mock<ProbeContext>();
                _probeContext.Setup(m => m.CreateScope("sagaRepository")).Returns(_scope.Object);

                var repository = new RavenDbSagaRepository<SimpleSaga>(store, null);

                repository.Probe(_probeContext.Object);
            }
        }
    }
}