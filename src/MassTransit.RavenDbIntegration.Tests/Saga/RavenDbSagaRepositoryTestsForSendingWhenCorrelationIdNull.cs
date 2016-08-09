using System;
using System.Threading.Tasks;
using MassTransit.Pipeline;
using MassTransit.RavenDbIntegration.Saga;
using MassTransit.RavenDbIntegration.Tests.Saga.Messages;
using MassTransit.RavenDbIntegration.Tests.Saga.Sagas;
using MassTransit.Saga;
using Moq;
using NUnit.Framework;
using Raven.Client;

namespace MassTransit.RavenDbIntegration.Tests.Saga
{
    [TestFixture]
    public class RavenDbSagaRepositoryTestsForSendingWhenCorrelationIdNull
    {
        [Test]
        public void ThenSagaExceptionThrown()
        {
            Assert.That(_exception, Is.Not.Null);
        }

        SagaException _exception;

        [OneTimeSetUp]
        public async Task GivenARavenDbSagaRepository_WhenSendingWithNullCorrelationId()
        {
            var context = new Mock<ConsumeContext<InitiateSimpleSaga>>();
            context.Setup(x => x.CorrelationId).Returns(default(Guid?));

            var repository = new RavenDbSagaRepository<SimpleSaga>(Mock.Of<IDocumentStore>(), null);

            try
            {
                await repository.Send(context.Object, Mock.Of<ISagaPolicy<SimpleSaga, InitiateSimpleSaga>>(),
                        Mock.Of<IPipe<SagaConsumeContext<SimpleSaga, InitiateSimpleSaga>>>());
            }
            catch (SagaException exception)
            {
                _exception = exception;
            }
        }
    }
}