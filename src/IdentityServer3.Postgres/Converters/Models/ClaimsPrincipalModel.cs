namespace IdentityServer3.Postgres.Converters.Models
{
    internal class ClaimsPrincipalModel
    {
        public string AuthenticationType { get; set; }

        public ClaimModel[] Claims { get; set; }
    }
}
