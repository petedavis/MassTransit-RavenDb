using System;
using System.Threading;
using System.Threading.Tasks;
using MassTransit.RavenDbIntegration.Saga;
using MassTransit.RavenDbIntegration.Saga.Context;
using MassTransit.RavenDbIntegration.Tests.Saga.Messages;
using MassTransit.RavenDbIntegration.Tests.Saga.Sagas;
using MassTransit.Saga;
using Moq;
using NUnit.Framework;
using Raven.Abstractions.Data;
using Raven.Client;
using Raven.Client.Embedded;
using Raven.Tests.Helpers;

namespace MassTransit.RavenDbIntegration.Tests.Saga
{
    [TestFixture]
    public class RavenDbSagaRepositoryTestsForSendingWhenPolicyReturnsCompletedInstance : RavenTestBase
    {
        [Test]
        public async Task ThenTheCompletedSagaIsNotUpdated()
        {
            using (var session = _documentStore.OpenAsyncSession())
            {
                var actual = await session.LoadAsync<SimpleSaga>(_simpleSaga.Id, _cancellationToken);

                var etag = session.Advanced.GetEtagFor(actual);
                Assert.That(etag, Is.EqualTo(_etag));
            }

        }

        SimpleSaga _simpleSaga;
        CancellationToken _cancellationToken;
        Guid _correlationId;
        EmbeddableDocumentStore _documentStore;
        private Etag _etag;

        [OneTimeSetUp]
        public async Task GivenARavenDbSagaRespository_WhenSendingCompletedInstance()
        {
            _correlationId = Guid.NewGuid();
            _cancellationToken = new CancellationToken();

            var context = new Mock<ConsumeContext<CompleteSimpleSaga>>();
            context.Setup(x => x.CorrelationId).Returns(_correlationId);
            context.Setup(m => m.CancellationToken).Returns(_cancellationToken);

            _simpleSaga = new SimpleSaga
            {
                CorrelationId = _correlationId,
            };

            await _simpleSaga.Consume(It.IsAny<ConsumeContext<CompleteSimpleSaga>>());

            _documentStore = NewDocumentStore();

            var sagaConsumeContext = new Mock<SagaConsumeContext<SimpleSaga, CompleteSimpleSaga>>();
            sagaConsumeContext.SetupGet(x => x.IsCompleted).Returns(true);
            var ravenDbSagaConsumeContextFactory = new Mock<IRavenDbSagaConsumeContextFactory>();
            ravenDbSagaConsumeContextFactory.Setup(
                x => x.Create(It.IsAny<IAsyncDocumentSession>(), context.Object, It.IsAny<SimpleSaga>(), true))
                .Returns(sagaConsumeContext.Object);

            using (var session = _documentStore.OpenAsyncSession())
            {
                await session.StoreAsync(_simpleSaga, _cancellationToken);
                await session.SaveChangesAsync(_cancellationToken);
                _etag = session.Advanced.GetEtagFor(_simpleSaga);
            }

            var repository = new RavenDbSagaRepository<SimpleSaga>(_documentStore, ravenDbSagaConsumeContextFactory.Object);
            await repository.Send(context.Object, Mock.Of<ISagaPolicy<SimpleSaga, CompleteSimpleSaga>>(), null);
        }

        [OneTimeTearDown]
        public void Kill()
        {
            _documentStore.Dispose();
        }
    }
}