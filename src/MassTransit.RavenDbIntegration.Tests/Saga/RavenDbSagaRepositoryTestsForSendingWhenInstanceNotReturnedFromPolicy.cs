using System;
using System.Threading;
using System.Threading.Tasks;
using MassTransit.Pipeline;
using MassTransit.RavenDbIntegration.Saga;
using MassTransit.RavenDbIntegration.Saga.Context;
using MassTransit.RavenDbIntegration.Tests.Saga.Messages;
using MassTransit.RavenDbIntegration.Tests.Saga.Sagas;
using MassTransit.Saga;
using Moq;
using NUnit.Framework;
using Raven.Client;
using Raven.Tests.Helpers;

namespace MassTransit.RavenDbIntegration.Tests.Saga
{
    [TestFixture]
    public class RavenDbSagaRepositoryTestsForSendingWhenInstanceNotReturnedFromPolicy : RavenTestBase
    {
        [Test]
        public void ThenPolicyUpdatedWithSagaInstance()
        {
            _policy.Verify(m => m.Existing(_sagaConsumeContext.Object, _nextPipe.Object));
        }

        [Test]
        public void ThenPreInsertInstanceCalledToGetInstance()
        {
            _policy.Verify(m => m.PreInsertInstance(_context.Object, out _nullSimpleSaga));
        }

        Mock<ISagaPolicy<SimpleSaga, InitiateSimpleSaga>> _policy;
        Mock<ConsumeContext<InitiateSimpleSaga>> _context;
        SimpleSaga _nullSimpleSaga;
        Guid _correlationId;
        CancellationToken _cancellationToken;
        Mock<IPipe<SagaConsumeContext<SimpleSaga, InitiateSimpleSaga>>> _nextPipe;
        SimpleSaga _simpleSaga;
        Mock<IRavenDbSagaConsumeContextFactory> _sagaConsumeContextFactory;
        Mock<SagaConsumeContext<SimpleSaga, InitiateSimpleSaga>> _sagaConsumeContext;

        [OneTimeSetUp]
        public async Task GivenARavenDbSagaRepository_WhenSendingAndInstanceNotReturnedFromPolicy()
        {
                _correlationId = Guid.NewGuid();
                _cancellationToken = new CancellationToken();

                _context = new Mock<ConsumeContext<InitiateSimpleSaga>>();
                _context.Setup(x => x.CorrelationId).Returns(_correlationId);
                _context.Setup(m => m.CancellationToken).Returns(_cancellationToken);

                _nullSimpleSaga = null;

                _policy = new Mock<ISagaPolicy<SimpleSaga, InitiateSimpleSaga>>();
                _policy.Setup(x => x.PreInsertInstance(_context.Object, out _nullSimpleSaga)).Returns(false);

                _nextPipe = new Mock<IPipe<SagaConsumeContext<SimpleSaga, InitiateSimpleSaga>>>();

                _simpleSaga = new SimpleSaga {CorrelationId = _correlationId};

                _sagaConsumeContext = new Mock<SagaConsumeContext<SimpleSaga, InitiateSimpleSaga>>();
                _sagaConsumeContext.Setup(x => x.CorrelationId).Returns(_correlationId);

                _sagaConsumeContextFactory = new Mock<IRavenDbSagaConsumeContextFactory>();
                _sagaConsumeContextFactory.Setup(
                    m => m.Create(It.IsAny<IAsyncDocumentSession>(), _context.Object,
                    It.Is<SimpleSaga>(x => x.CorrelationId == _correlationId), true))
                    .Returns(_sagaConsumeContext.Object);

            using (var store = NewDocumentStore(seedData: new [] { new []{_simpleSaga}}))
            {
                var repository = new RavenDbSagaRepository<SimpleSaga>(store, _sagaConsumeContextFactory.Object);

                await repository.Send(_context.Object, _policy.Object, _nextPipe.Object);
            }
        }
    }
}