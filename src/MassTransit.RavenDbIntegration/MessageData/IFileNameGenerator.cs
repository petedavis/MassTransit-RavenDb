namespace MassTransit.RavenDbIntegration.MessageData
{
    public interface IFileNameGenerator
    {
        string GeneratePath();
    }
}