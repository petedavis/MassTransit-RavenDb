﻿namespace MassTransit.Persistance.RavenDbIntegration.Tests.Sagas
{
    using NUnit.Framework;

    [TestFixture, Category("Integration")]
    public class SagaLoadTest //: LoopbackTestFixture <- in the tests project
    {
        [Test]
        public void Put_some_stress_on_the_saga_dispatcher_to_see_how_it_handles_multiple_sagas_at_once()
        {
        }
    }
}