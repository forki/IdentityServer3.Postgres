namespace IdentityServer3.Postgres.Converters
{
    using System;
    using System.Linq;
    using System.Security.Claims;

    using IdentityServer3.Postgres.Converters.Models;

    using Newtonsoft.Json;

    public class ClaimsPrincipalConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var source = (ClaimsPrincipal)value;

            var target = new ClaimsPrincipalModel()
            {
                AuthenticationType = source.Identity.AuthenticationType,
                Claims = source.Claims
                                .Select(x => new ClaimModel
                                {
                                    Type = x.Type,
                                    Value = x.Value,
                                    ValueType = x.ValueType
                                }).ToArray()
            };

            serializer.Serialize(target);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var source = serializer.Deserialize<ClaimsPrincipalModel>(reader);

            var claims = source.Claims.Select(x => new Claim(x.Type, x.Value, x.ValueType));
            var id = new ClaimsIdentity(claims, source.AuthenticationType);
            var target = new ClaimsPrincipal(id);

            return target;
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(ClaimsPrincipal);
        }
    }
}
