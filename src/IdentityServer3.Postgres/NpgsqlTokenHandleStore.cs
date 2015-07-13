namespace IdentityServer3.Postgres
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Npgsql;

    using Thinktecture.IdentityServer.Core.Models;
    using Thinktecture.IdentityServer.Core.Services;

    public class NpgsqlTokenHandleStore : BaseNpgsqlTokenStore<Token>, ITokenHandleStore
    {
        public NpgsqlTokenHandleStore(NpgsqlConnection conn, string schema, IScopeStore scopeStore, IClientStore clientStore)
            : base(conn, schema, TokenType.TokenHandle, scopeStore, clientStore)
        {
        }

        protected override Token ToToken(string key, Thinktecture.IdentityServer.Core.Models.Token value)
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

        public async Task StoreAsync(string key, Thinktecture.IdentityServer.Core.Models.Token value)
        {
            var token = ToToken(key, value);

            await InsertAsync(token);
        }
    }
}