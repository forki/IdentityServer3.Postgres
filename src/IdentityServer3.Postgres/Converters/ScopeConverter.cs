namespace IdentityServer3.Postgres.Converters
{
    using System;
    using System.Linq;

    using IdentityServer3.Postgres.Converters.Models;

    using Newtonsoft.Json;

    using Thinktecture.IdentityServer.Core.Models;
    using Thinktecture.IdentityServer.Core.Services;

    public class ScopeConverter : JsonConverter
    {
        private readonly IScopeStore _scopeStore;

        public ScopeConverter(IScopeStore scopeStore)
        {
            _scopeStore = scopeStore;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var source = (Scope) value;

            var target = new ScopeModel
            {
                Name = source.Name
            };

            serializer.Serialize(writer, target);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var source = serializer.Deserialize<ScopeModel>(reader);

            var scopes = _scopeStore.FindScopesAsync(new[] {source.Name}).GetAwaiter().GetResult();

            return scopes.Single();
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Scope);
        }
    }
}
