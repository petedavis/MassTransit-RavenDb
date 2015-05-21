using System;
using System.Diagnostics;
using System.Threading;
using MassTransit.Logging;
using MassTransit.Saga;

namespace MassTransit.Persistence.RavenDB.Tests.Sagas
{
    public class ConcurrentLegacySaga :
        ISaga,
        InitiatedBy<StartConcurrentSaga>,
        Orchestrates<ContinueConcurrentSaga>
    {
        static readonly ILog _log = Logger.Get<ConcurrentLegacySaga>();


        public ConcurrentLegacySaga(Guid correlationId)
        {
            CorrelationId = correlationId;

            Value = -1;
        }

        protected ConcurrentLegacySaga()
        {
            Value = -1;
        }

        public virtual string Name { get; set; }
        public virtual int Value { get; set; }

        public virtual void Consume(StartConcurrentSaga message)
        {
            Trace.WriteLine("Consuming " + message.GetType());
            Thread.Sleep(3000);
            Name = message.Name;
            Value = message.Value;
            Trace.WriteLine("Completed " + message.GetType());
        }

        public virtual Guid CorrelationId { get; set; }
        public virtual IServiceBus Bus { get; set; }

        public virtual void Consume(ContinueConcurrentSaga message)
        {
            Trace.WriteLine("Consuming " + message.GetType());
            Thread.Sleep(1000);

            if (Value == -1)
                throw new InvalidOperationException("Should not be a -1 dude!!");

            Value = message.Value;
            Trace.WriteLine("Completed " + message.GetType());
        }
    }
}