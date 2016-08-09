using System;
using System.Threading.Tasks;
using MassTransit.RavenDbIntegration.Saga;
using MassTransit.RavenDbIntegration.Tests.Saga.Sagas;
using Raven.Client;

namespace MassTransit.RavenDbIntegration.Tests.Saga
{
    public static class DocumentStoreExtensions
    {

        public static async Task InsertSaga(this IDocumentStore store, SimpleSaga saga)
        {
            using (var session = store.OpenAsyncSession())
            {
                await session.StoreAsync(saga, saga.Id);
                await session.SaveChangesAsync();
            }
        }

        public static async Task<SimpleSaga> GetSaga(this IDocumentStore store, Guid correlationId)
        {
            using (var session = store.OpenAsyncSession())
            {
                return await session.LoadAsync<SimpleSaga>(SagaKeyProvider.GetKey<SimpleSaga>(correlationId));
            }
        }
    }
}