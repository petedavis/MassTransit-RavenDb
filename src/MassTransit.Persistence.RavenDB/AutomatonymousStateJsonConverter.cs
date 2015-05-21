using System;
using Automatonymous;
using Raven.Imports.Newtonsoft.Json;

namespace MassTransit.Persistence.RavenDB
{
    public class AutomatonymousStateJsonConverter<TSaga> : JsonConverter
        where TSaga : StateMachine
    {
        private readonly TSaga _saga;

        public AutomatonymousStateJsonConverter(TSaga saga)
        {
            _saga = saga;
        }

        public override bool CanConvert(Type objectType)
        {
            return typeof(State).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var stateName = serializer.Deserialize<string>(reader);
            var state = _saga.GetState(stateName);
            return state;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var state = (State) value;
            writer.WriteValue(state.Name);
        }
    }
}
