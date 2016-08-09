using MassTransit.Saga;

namespace MassTransit.RavenDbIntegration.Saga
{
    public interface IVersionedSaga :
        ISaga
    {
        int Version { get; set; }
    }
}