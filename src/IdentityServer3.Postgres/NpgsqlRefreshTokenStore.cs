namespace IdentityServer3.Postgres
{
    using System;
    using System.Threading.Tasks;

    using Npgsql;

    using Thinktecture.IdentityServer.Core.Models;
    using Thinktecture.IdentityServer.Core.Services;

    public class NpgsqlRefreshTokenStore : BaseNpgsqlTokenStore<RefreshToken>, IRefreshTokenStore
    {
        private readonly string _updateExpiryQuery;

        public NpgsqlRefreshTokenStore(NpgsqlConnection conn, IScopeStore scopeStore, IClientStore clientStore)
            : this(conn, "public", scopeStore, clientStore)
        {
        }

        public NpgsqlRefreshTokenStore(NpgsqlConnection conn, string schema, IScopeStore scopeStore, IClientStore clientStore)
            : base(conn, schema, TokenType.RefreshToken, scopeStore, clientStore)
        {
            _updateExpiryQuery = $"UPDATE {Schema}.tokens " +
                                 "SET expiry = @expiry " +
                                 "WHERE key = @key " +
                                 " AND token_type = @tokenType";
        }

        protected override Token ToToken(string key, RefreshToken value)
        {
            return new Token
            {
                Key = key,
                ClientId = value.ClientId,
                SubjectId = value.SubjectId,
                TokenType = TokenType.RefreshToken,
                Model = ConvertToJson(value)
            };
        }

        public async Task StoreAsync(string key, RefreshToken value)
        {
            var fromDb = await GetAsync(key);
            var expiry = DateTimeOffset.UtcNow.AddSeconds(value.LifeTime);

            if (fromDb == null)
            {
                var token = ToToken(key, value);
                token.Expiry = expiry;

                await InsertAsync(token);
            }
            else
            {
                await Conn.ExecuteCommand(_updateExpiryQuery,
                    async cmd =>
                    {
                        cmd.Parameters.AddWithValue("expiry", expiry);
                        cmd.Parameters.AddWithValue("key", key);
                        cmd.Parameters.AddWithValue("token_type", (short) StoredTokenType);

                        int rowsAffected = await cmd.ExecuteNonQueryAsync();

                        return rowsAffected;
                    });
            }
        }
    }
}