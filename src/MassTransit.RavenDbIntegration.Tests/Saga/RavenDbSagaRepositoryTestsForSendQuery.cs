using System;
using System.Threading.Tasks;
using MassTransit.Pipeline;
using MassTransit.RavenDbIntegration.Saga;
using MassTransit.RavenDbIntegration.Saga.Context;
using MassTransit.RavenDbIntegration.Saga.Pipeline;
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
    public class RavenDbSagaRepositoryTestsForSendQuery : RavenTestBase
    {
        [Test]
        public void ThenMissingPipeNotCalled()
        {
            _sagaPolicy.Verify(x => x.Missing(_sagaQueryConsumeContext.Object, It.IsAny<MissingPipe<SimpleSaga, InitiateSimpleSaga>>()), Times.Never);
        }

        [Test]
        public void ThenSagaSentToInstance()
        {
            _sagaPolicy.Verify(x => x.Existing(_sagaConsumeContext.Object, _nextPipe.Object));
        }

        Guid _correlationId;
        Mock<ISagaPolicy<SimpleSaga, InitiateSimpleSaga>> _sagaPolicy;
        Mock<SagaQueryConsumeContext<SimpleSaga, InitiateSimpleSaga>> _sagaQueryConsumeContext;
        Mock<IPipe<SagaConsumeContext<SimpleSaga, InitiateSimpleSaga>>> _nextPipe;
        Mock<SagaConsumeContext<SimpleSaga, InitiateSimpleSaga>> _sagaConsumeContext;
        Mock<IRavenDbSagaConsumeContextFactory> _sagaConsumeContextFactory;
        IDocumentStore _documentStore;

        [OneTimeSetUp]
        public async Task GivenARavenDbSagaRepository_WhenSendingQuery()
        {
            _documentStore = NewDocumentStore();
            _correlationId = Guid.NewGuid();
            var saga = new SimpleSaga { CorrelationId = _correlationId };

            await _documentStore.InsertSaga(saga);

            _sagaQueryConsumeContext = new Mock<SagaQueryConsumeContext<SimpleSaga, InitiateSimpleSaga>>();
            _sagaQueryConsumeContext.Setup(x => x.Query.FilterExpression).Returns(x => x.CorrelationId == _correlationId);
            _sagaPolicy = new Mock<ISagaPolicy<SimpleSaga, InitiateSimpleSaga>>();
            _nextPipe = new Mock<IPipe<SagaConsumeContext<SimpleSaga, InitiateSimpleSaga>>>();

            _sagaConsumeContext = new Mock<SagaConsumeContext<SimpleSaga, InitiateSimpleSaga>>();
            _sagaConsumeContext.Setup(x => x.CorrelationId).Returns(_correlationId);

            _sagaConsumeContextFactory = new Mock<IRavenDbSagaConsumeContextFactory>();
            _sagaConsumeContextFactory.Setup(
                m =>
                    m.Create(It.IsAny<IAsyncDocumentSession>(), _sagaQueryConsumeContext.Object,
                        It.Is<SimpleSaga>(x => x.CorrelationId == _correlationId), true)).Returns(_sagaConsumeContext.Object);

            var repository = new RavenDbSagaRepository<SimpleSaga>(_documentStore, _sagaConsumeContextFactory.Object);

            await repository.SendQuery(_sagaQueryConsumeContext.Object, _sagaPolicy.Object, _nextPipe.Object);
        }

        [OneTimeTearDown]
        public void Kill()
        {
            _documentStore.Dispose();
        }
    }
}