namespace IdentityServer3.Postgres
{
    using System;
    using System.Collections.Generic;
    using System.Data.Common;
    using System.Linq;
    using System.Threading.Tasks;

    using IdentityServer3.Postgres.Converters;

    using Newtonsoft.Json;

    using Npgsql;

    using Thinktecture.IdentityServer.Core.Models;
    using Thinktecture.IdentityServer.Core.Services;

    public abstract class BaseNpgsqlTokenStore<T> : IDisposable where T : class
    {
        protected readonly NpgsqlConnection Conn;
        protected readonly TokenType StoredTokenType;
        private readonly IScopeStore _scopeStore;
        private readonly IClientStore _clientStore;
        private readonly JsonSerializer _serializer;

        protected readonly string Schema;

        private readonly string _getQuery;
        private readonly string _getAllQuery;
        private readonly string _insertQuery;
        private readonly string _removeQuery;
        private readonly string _revokeQuery;

        protected BaseNpgsqlTokenStore(NpgsqlConnection conn, string schema, 
            TokenType storedTokenType, IScopeStore scopeStore, IClientStore clientStore)
        {
            StoredTokenType = storedTokenType;

            Conn = conn;
            Schema = schema;

            _serializer = JsonSerializerFactory.Create();
            _serializer.Converters.Add(new ClaimConverter());
            _serializer.Converters.Add(new ClaimsPrincipalConverter());
            _serializer.Converters.Add(new ClientConverter(clientStore));
            _serializer.Converters.Add(new ScopeConverter(scopeStore));

            _scopeStore = scopeStore;
            _clientStore = clientStore;


            // Queries
            _revokeQuery = $"DELETE FROM {Schema}.tokens " +
                           "WHERE subject = @subject " +
                           " AND client = @client " +
                           " AND token_type = @tokenType";
            _removeQuery = $"DELETE FROM {Schema}.tokens " +
                           "WHERE key = @key " +
                           " AND token_type = @tokenType";

            _getQuery = $"SELECT * FROM {Schema}.tokens " +
                        "WHERE key = @key " +
                        " AND token_type = @tokenType";

            _getAllQuery = $"SELECT * FROM {Schema}.tokens " +
                           "WHERE subject = @subject " +
                           " AND token_type = @tokenType";

            _insertQuery = "INSERT INTO " +
                         $"{Schema}.tokens(key, token_type, subject, client, expiry, model) " +
                         "VALUES(@key, @token_type, @subject, @client, @expiry, @model); ";
        }

        protected abstract Token ToToken(string key, T value);

        protected virtual T FromToken(Token t)
        {
            return _serializer.Deserialize<T>(t.Model);
        }

        protected string ConvertToJson(T value)
        {
            return _serializer.Serialize(value);
        }

        protected T ConvertFromJson(string json)
        {
            return _serializer.Deserialize<T>(json);
        }

        protected Task InsertAsync(Token token)
        {
            return Conn.ExecuteCommand(_insertQuery,
                async cmd =>
                {
                    cmd.Parameters.AddWithValue("key", token.Key);
                    cmd.Parameters.AddWithValue("token_type", StoredTokenType);
                    cmd.Parameters.AddWithValue("subject", token.SubjectId);
                    cmd.Parameters.AddWithValue("client", token.ClientId);
                    cmd.Parameters.AddWithValue("expiry", token.Expiry);
                    cmd.Parameters.AddWithValue("model", token.Model);

                    int rowsAffected = await cmd.ExecuteNonQueryAsync();

                    return rowsAffected;
                });
        }

        public Task<T> GetAsync(string key)
        {
            return Conn.ExecuteCommand(_getQuery,
                async cmd =>
                {
                    cmd.Parameters.AddWithValue("key", key);
                    cmd.Parameters.AddWithValue("tokenType", StoredTokenType);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (reader.Read())
                        {
                            var token = Token.FromReader(reader);

                            if (token.Expiry >= DateTimeOffset.UtcNow)
                            {
                                return FromToken(token);
                            }
                        }

                        return null;
                    }
                });
        }

        public Task<IEnumerable<ITokenMetadata>> GetAllAsync(string subject)
        {
            return Conn.ExecuteCommand(_getAllQuery,
                async cmd =>
                {
                    cmd.Parameters.AddWithValue("subject", subject);
                    cmd.Parameters.AddWithValue("tokenType", StoredTokenType);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        var hasMoreRowsTask = reader.ReadAsync();
                        var resultList = new List<T>();

                        while (await hasMoreRowsTask)
                        {
                            var token = Token.FromReader(reader);

                            hasMoreRowsTask = reader.ReadAsync();

                            resultList.Add(FromToken(token));
                        }

                        return resultList.Cast<ITokenMetadata>();
                    }
                });
        }

        public Task RemoveAsync(string key)
        {
            return Conn.ExecuteCommand(_removeQuery,
                async cmd =>
                {
                    cmd.Parameters.AddWithValue("key", key);
                    cmd.Parameters.AddWithValue("tokenType", (short)StoredTokenType);

                    return await cmd.ExecuteNonQueryAsync();
                });
        }

        public Task RevokeAsync(string subject, string client)
        {
            return Conn.ExecuteCommand(_revokeQuery,
                async cmd =>
                {
                    cmd.Parameters.AddWithValue("subject", subject);
                    cmd.Parameters.AddWithValue("client", client);
                    cmd.Parameters.AddWithValue("tokenType", (short)StoredTokenType);

                    int rowsAffected = await cmd.ExecuteNonQueryAsync();

                    return rowsAffected;
                });
        }

        public void InitializeTable()
        {
            /*
            CREATE TABLE {_schema}.tokens
(
  key text NOT NULL,
  token_type smallint NOT NULL,
  subject character varying(255) NOT NULL,
  client character varying(255) NOT NULL,
  expiry timestamp with time zone NOT NULL,
  model jsonb NOT NULL,
  CONSTRAINT pk_tokens_key_tokentype PRIMARY KEY (token_type, key)
)
WITH (
  OIDS=FALSE
)

CREATE INDEX ix_tokens_subject_client_tokentype
  ON {_schema}.tokens
  USING btree
  (subject COLLATE pg_catalog."default", client COLLATE pg_catalog."default", token_type);
            */
        }

        public enum TokenType : short
        {
            AuthorizationCode = 1,
            TokenHandle = 2,
            RefreshToken = 3
        }

        public class Token
        {
            public static Token FromReader(DbDataReader reader)
            {
                int keyOrdinal = reader.GetOrdinal("key");
                int tokenTypeOrdinal = reader.GetOrdinal("tokenType");
                int subjectOrdinal = reader.GetOrdinal("subject");
                int clientOrdinal = reader.GetOrdinal("client");
                int expiryOrdinal = reader.GetOrdinal("expiry");
                int modelOrdinal = reader.GetOrdinal("model");

                return new Token
                {
                    Key = reader.GetString(keyOrdinal),
                    TokenType = (TokenType) reader.GetInt16(tokenTypeOrdinal),
                    SubjectId = reader.GetString(subjectOrdinal),
                    ClientId = reader.GetString(clientOrdinal),
                    Expiry = new DateTimeOffset(reader.GetDateTime(expiryOrdinal)),
                    Model = reader.GetString(modelOrdinal),
                };
            }

            public string Key { get; set; }

            public TokenType TokenType { get; set; }

            public string SubjectId { get; set; }

            public string ClientId { get; set; }

            public DateTimeOffset Expiry { get; set; }

            public string Model { get; set; }

        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Conn?.Dispose();
                    ((IDisposable)_scopeStore)?.Dispose();
                    ((IDisposable)_clientStore)?.Dispose();
                }

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion
    }
}