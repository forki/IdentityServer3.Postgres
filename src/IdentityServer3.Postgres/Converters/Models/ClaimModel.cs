namespace IdentityServer3.Postgres.Converters.Models
{
    internal class ClaimModel
    {
        public string Type { get; set; }
        public string Value { get; set; }
        public string ValueType { get; set; }
    }
}