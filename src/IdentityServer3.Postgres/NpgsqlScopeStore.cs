namespace IdentityServer3.Postgres
{
    using System;
    using System.Collections.Generic;
    using System.Data.Common;
    using System.Threading.Tasks;

    using IdentityServer3.Postgres.Converters;

    using Newtonsoft.Json;

    using Npgsql;

    using NpgsqlTypes;

    using Thinktecture.IdentityServer.Core.Models;
    using Thinktecture.IdentityServer.Core.Services;

    public class NpgsqlScopeStore : IScopeStore, IDisposable
    {
        private readonly NpgsqlConnection _conn;
        private readonly JsonSerializer _serializer;

        private readonly string _schema;

        private readonly string _findQuery;
        private readonly string _getQuery;

        public NpgsqlScopeStore(NpgsqlConnection conn)
            : this(conn, "public")
        {

        }

        public NpgsqlScopeStore(NpgsqlConnection conn, NpgsqlSchema schema)
        {
            Preconditions.IsNotNull(conn, nameof(conn));
            Preconditions.IsShortString(schema, nameof(schema));

            _serializer = JsonSerializerFactory.Create();

            _conn = conn;
            _schema = schema;

            _findQuery = "SELECT name, is_public, model " +
                         $"FROM {_schema}.scopes " +
                         "WHERE name = any (@names)";

            _getQuery = "SELECT name, is_public, model " +
                        $"FROM {_schema}.scopes " +
                        "WHERE is_public = @public";
        }

        public Task<IEnumerable<Scope>> FindScopesAsync(IEnumerable<string> scopeNames)
        {
            return _conn.ExecuteCommand(_findQuery,
                async cmd =>
                {
                    var paramType = NpgsqlDbType.Array | NpgsqlDbType.Text;
                    var param = new NpgsqlParameter("names", paramType)
                    {
                        NpgsqlValue = scopeNames
                    };

                    cmd.Parameters.Add(param);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        var result = await ParseReader(reader);

                        return result;
                    }
                });
        }

        public Task<IEnumerable<Scope>> GetScopesAsync(bool publicOnly = true)
        {
            return _conn.ExecuteCommand(_getQuery,
                async cmd =>
                {
                    cmd.Parameters.AddWithValue("public", publicOnly);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        var result = await ParseReader(reader);

                        return result;
                    }
                });
        }

        public Task SaveScopeAsync(Scope scope)
        {
            string query = $"INSERT INTO {_schema}.scopes(name, is_public, model) " +
                           "VALUES (@name, @public, @model)";

            return _conn.ExecuteCommand(query,
                async cmd =>
                {
                    var serialized = _serializer.Serialize(scope);

                    cmd.Parameters.AddWithValue("name", scope.Name);
                    cmd.Parameters.AddWithValue("public", scope.ShowInDiscoveryDocument);
                    cmd.Parameters.AddWithValue("model", serialized);

                    await cmd.ExecuteNonQueryAsync();

                    return 0;
                });
        }

        public void InitializeTable()
        {
            string query = $"CREATE TABLE {_schema}.scopes" +
                           "(" +
                           "name text NOT NULL," +
                           "is_public boolean NOT NULL," +
                           "model jsonb NOT NULL," +
                           "CONSTRAINT pk_scopes_name PRIMARY KEY(name)" +
                           ") WITH(OIDS = FALSE)";

            _conn.ExecuteCommand(query,
                async cmd =>
                {
                    await cmd.ExecuteNonQueryAsync();
                    return 0;
                }).GetAwaiter().GetResult();
        }

        private async Task<IEnumerable<Scope>> ParseReader(DbDataReader reader)
        {
            var resultList = new List<Scope>();

            var hasMoreRows = reader.ReadAsync();

            while (await hasMoreRows)
            {
                int scopeOrdinal = reader.GetOrdinal("model");
                string model = reader.GetString(scopeOrdinal);

                hasMoreRows = reader.ReadAsync();

                var scope = _serializer.Deserialize<Scope>(model);

                resultList.Add(scope);
            }

            return resultList;
        }

        public void Dispose()
        {
            ((IDisposable)_conn).Dispose();
        }
    }
}