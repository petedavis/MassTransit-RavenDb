using MassTransit.Saga;

namespace MassTransit.RavenDbIntegration.Saga
{
    public interface ISagaDocument : ISaga
    {
        string Id { get; }
    }
}