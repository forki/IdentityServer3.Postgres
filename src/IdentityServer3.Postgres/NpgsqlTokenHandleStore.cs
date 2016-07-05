namespace IdentityServer3.Postgres
{
    using System;
    using System.Threading.Tasks;

    using Npgsql;

    using IdentityServer3.Core.Models;
    using IdentityServer3.Core.Services;

    public class NpgsqlTokenHandleStore : BaseNpgsqlTokenStore<Token>, ITokenHandleStore
    {
        public NpgsqlTokenHandleStore(NpgsqlConnection conn, NpgsqlSchema schema, 
            IScopeStore scopeStore, IClientStore clientStore)
            : base(conn, schema, TokenType.TokenHandle, scopeStore, clientStore)
        {
        }

        protected override Token ToToken(string key, IdentityServer3.Core.Models.Token value)
        {
            var token = new Token
            {
                Key = key,
                SubjectId = value.SubjectId,
                ClientId = value.ClientId,
                Model = ConvertToJson(value),
                Expiry = DateTimeOffset.UtcNow.AddSeconds(value.Lifetime),
                TokenType = this.StoredTokenType
            };

            return token;
        }

        public async Task StoreAsync(string key, IdentityServer3.Core.Models.Token value)
        {
            var token = ToToken(key, value);

            await InsertAsync(token);
        }
    }
}