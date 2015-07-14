namespace IdentityServer3.Postgres
{
    using System.IO;

    using IdentityServer3.Postgres.Converters;

    using Newtonsoft.Json;

    public static class JsonSerializerFactory
    {
        public static JsonSerializer Create()
        {
            var serializer = new JsonSerializer()
            {
                DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
                Formatting = Formatting.Indented,
                TypeNameHandling = TypeNameHandling.Objects,
            };

            serializer.Converters.Add(new ClaimConverter());

            return serializer;
        }
    }

    internal static class JsonSerializerExtensions
    {
        public static string Serialize(this JsonSerializer serializer, object value)
        {
            using (var writer = new StringWriter())
            {
                serializer.Serialize(writer, value);

                writer.Flush();

                return writer.ToString();
            }
        }

        public static T Deserialize<T>(this JsonSerializer serializer, string value)
        {
            using (var reader = new JsonTextReader(new StringReader(value)))
            {
                return serializer.Deserialize<T>(reader);
            }
        }
    }
}