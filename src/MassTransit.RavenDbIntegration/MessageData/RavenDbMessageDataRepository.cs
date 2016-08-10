using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MassTransit.Logging;
using MassTransit.MessageData;
using Raven.Client.FileSystem;
using Raven.Json.Linq;

namespace MassTransit.RavenDbIntegration.MessageData
{
    public class RavenDbMessageDataRepository :
        IMessageDataRepository
    {
        private readonly IFilesStore _bucket;
        private readonly IFileNameGenerator _fileNameGenerator;
        private readonly ILog _log = Logger.Get<RavenDbMessageDataRepository>();
        private readonly IMessageDataResolver _resolver;

        public RavenDbMessageDataRepository(IMessageDataResolver resolver, IFilesStore bucket,
            IFileNameGenerator fileNameGenerator)
        {
            _resolver = resolver;
            _bucket = bucket;
            _fileNameGenerator = fileNameGenerator;
        }

        async Task<Stream> IMessageDataRepository.Get(Uri address, CancellationToken cancellationToken)
        {
            var path = _resolver.GetPath(address);

            using (var session = _bucket.OpenAsyncSession())
            {
                return await session.DownloadAsync(path).ConfigureAwait(false);
            }
        }

        async Task<Uri> IMessageDataRepository.Put(Stream stream, TimeSpan? timeToLive,
            CancellationToken cancellationToken)
        {
            var metadata = CreateMetadata(timeToLive);

            var path = _fileNameGenerator.GeneratePath();

            using (var session = _bucket.OpenAsyncSession())
            {
                session.RegisterUpload(path, stream, metadata);

                await session.SaveChangesAsync().ConfigureAwait(false);

                if (_log.IsDebugEnabled)
                    _log.DebugFormat("MessageData:Put {0}", path);

                return _resolver.GetAddress(path);
            }
        }

        private RavenJObject CreateMetadata(TimeSpan? timeToLive)
        {
            var metadata = new RavenJObject();

            if (timeToLive.HasValue)
            {
                metadata["expiration"] = DateTime.UtcNow.Add(timeToLive.Value);
            }

            return metadata;
        }
    }
}