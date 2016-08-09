using System;
using System.Threading;
using System.Threading.Tasks;
using MassTransit.Pipeline;
using MassTransit.RavenDbIntegration.Saga;
using MassTransit.RavenDbIntegration.Saga.Pipeline;
using MassTransit.RavenDbIntegration.Tests.Saga.Messages;
using MassTransit.RavenDbIntegration.Tests.Saga.Sagas;
using MassTransit.Saga;
using Moq;
using NUnit.Framework;
using Raven.Tests.Helpers;

namespace MassTransit.RavenDbIntegration.Tests.Saga
{
    [TestFixture]
    public class RavenDbSagaRepositoryTestsForSendingWhenInstanceNotFound : RavenTestBase
    {
        [Test]
        public void ThenMissingPipeInvokedOnPolicy()
        {
            _policy.Verify(m => m.Missing(_context.Object, It.IsAny<MissingPipe<SimpleSaga, InitiateSimpleSaga>>()));
        }

        Mock<ISagaPolicy<SimpleSaga, InitiateSimpleSaga>> _policy;
        Mock<ConsumeContext<InitiateSimpleSaga>> _context;
        SimpleSaga _nullSimpleSaga;
        Mock<IPipe<SagaConsumeContext<SimpleSaga, InitiateSimpleSaga>>> _nextPipe;

        [OneTimeSetUp]
        public async Task GivenARavenDbSagaRepository_WhenSendingAndInstanceNotFound()
        {
            using (var store = NewDocumentStore())
            {
                _context = new Mock<ConsumeContext<InitiateSimpleSaga>>();
                _context.Setup(x => x.CorrelationId).Returns(It.IsAny<Guid>());
                _context.Setup(m => m.CancellationToken).Returns(It.IsAny<CancellationToken>());

                _nullSimpleSaga = null;

                _policy = new Mock<ISagaPolicy<SimpleSaga, InitiateSimpleSaga>>();
                _policy.Setup(x => x.PreInsertInstance(_context.Object, out _nullSimpleSaga)).Returns(false);

                _nextPipe = new Mock<IPipe<SagaConsumeContext<SimpleSaga, InitiateSimpleSaga>>>();

                var repository = new RavenDbSagaRepository<SimpleSaga>(store, null);

                await repository.Send(_context.Object, _policy.Object, _nextPipe.Object);
            }
        }
    }
}