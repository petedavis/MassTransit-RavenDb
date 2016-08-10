using System;

namespace MassTransit.RavenDbIntegration.MessageData
{
    public class MessageDataResolver :
        IMessageDataResolver
    {
        private const string Scheme = "urn";
        private const string System = "ravendb";
        private const string Specification = "ravenfs";

        private readonly string _format = string.Join(":", Scheme, System, Specification);

        public string GetPath(Uri address)
        {
            if (address.Scheme != Scheme)
                throw new UriFormatException($"The scheme did not match the expected value: {Scheme}");

            var tokens = address.AbsolutePath.Split(':');

            if (tokens.Length != 3 || !address.AbsoluteUri.StartsWith($"{_format}:"))
                throw new UriFormatException($"Urn is not in the correct format. Use '{_format}:{{storagePath}}'");

            return tokens[2];
        }

        public Uri GetAddress(string path)
        {
            return new Uri($"{_format}:{path}");
        }
    }
}