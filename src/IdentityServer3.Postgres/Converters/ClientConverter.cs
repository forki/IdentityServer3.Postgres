namespace IdentityServer3.Postgres.Converters
{
    using System;
    using System.Threading.Tasks;

    using IdentityServer3.Postgres.Converters.Models;

    using Newtonsoft.Json;

    using IdentityServer3.Core.Models;
    using IdentityServer3.Core.Services;

    public class ClientConverter : JsonConverter
    {
        private readonly IClientStore _clientStore;

        public ClientConverter(IClientStore clientStore)
        {
            _clientStore = clientStore;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var source = (Client) value;

            var target = new ClientModel
            {
                ClientId = source.ClientId
            };

            serializer.Serialize(writer, target);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var source = serializer.Deserialize<ClientModel>(reader);

            var client = _clientStore.FindClientByIdAsync(source.ClientId).GetAwaiter().GetResult();

            return client;
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Client);
        }
    }
}