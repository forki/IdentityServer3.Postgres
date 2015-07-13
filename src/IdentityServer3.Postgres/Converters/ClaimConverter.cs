namespace IdentityServer3.Postgres.Converters
{
    using System;
    using System.Security.Claims;

    using IdentityServer3.Postgres.Converters.Models;

    using Newtonsoft.Json;

    public class ClaimConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            Claim source = (Claim)value;

            var target = new ClaimModel
            {
                Type = source.Type,
                Value = source.Value,
                ValueType = source.ValueType,
            };

            serializer.Serialize(writer, target);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var source = serializer.Deserialize<ClaimModel>(reader);
            var target = new Claim(source.Type, source.Value, source.ValueType);

            return target;
        }

        public override bool CanConvert(Type objectType)
        {
            return typeof(Claim) == objectType;
        }
    }
}