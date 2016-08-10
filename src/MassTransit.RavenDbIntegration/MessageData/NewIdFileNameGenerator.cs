using MassTransit.NewIdFormatters;

namespace MassTransit.RavenDbIntegration.MessageData
{
    public class NewIdFileNameGenerator :
        IFileNameGenerator
    {
        private static readonly INewIdFormatter Formatter = new ZBase32Formatter();

        public string GeneratePath()
        {
            return $"/messagedata/{Formatter.Format(NewId.Next().ToSequentialGuid().ToByteArray())}";
        }
    }
}