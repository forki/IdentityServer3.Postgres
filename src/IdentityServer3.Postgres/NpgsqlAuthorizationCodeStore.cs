namespace IdentityServer3.Postgres
{
    using System;
    using System.Threading.Tasks;

    using Npgsql;

    using IdentityServer3.Core.Models;
    using IdentityServer3.Core.Services;

    public class NpgsqlAuthorizationCodeStore : BaseNpgsqlTokenStore<AuthorizationCode>, IAuthorizationCodeStore
    {
        public NpgsqlAuthorizationCodeStore(NpgsqlConnection conn,
                                            IScopeStore scopeStore,
                                            IClientStore clientStore)
            : this(conn, "public", scopeStore, clientStore)
        {

        }

        public NpgsqlAuthorizationCodeStore(NpgsqlConnection conn, NpgsqlSchema schema, IScopeStore scopeStore, IClientStore clientStore)
            : base(conn, schema, TokenType.AuthorizationCode, scopeStore, clientStore)
        {
        }

        protected override Token ToToken(string key, AuthorizationCode value)
        {
            return new Token
            {
                Key = key,
                ClientId = value.ClientId,
                SubjectId = value.SubjectId,
                TokenType = this.StoredTokenType,
                Expiry = DateTimeOffset.UtcNow.AddSeconds(value.Client.AuthorizationCodeLifetime),
                Model = ConvertToJson(value),
            };
        }

        public async Task StoreAsync(string key, AuthorizationCode value)
        {
            var token = ToToken(key, value);

            await InsertAsync(token);
        }
    }
}