namespace IdentityServer3.Postgres
{
    using System.Collections.Generic;
    using System.Data.Common;
    using System.Threading.Tasks;

    using Newtonsoft.Json;

    using Npgsql;

    using NpgsqlTypes;

    using Thinktecture.IdentityServer.Core.Models;
    using Thinktecture.IdentityServer.Core.Services;

    public class NpgsqlScopeStore : IScopeStore
    {
        private readonly NpgsqlConnection _conn;
        private readonly string _schema;

        private readonly string _findQuery;
        private readonly string _getQuery;

        public NpgsqlScopeStore(NpgsqlConnection conn)
            : this(conn, "public")
        {

        }

        public NpgsqlScopeStore(NpgsqlConnection conn, string schema)
        {
            Preconditions.IsNotNull(conn, nameof(conn));
            Preconditions.IsShortString(schema, nameof(schema));

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
                    var row = ScopeRow.FromScope(scope);
                    var serialized = JsonConvert.SerializeObject(row.Model);

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
                var row = ScopeRow.FromReader(reader);

                hasMoreRows = reader.ReadAsync();

                var scope = row.ToScope();
                resultList.Add(scope);
            }

            return resultList;
        }

        internal class ScopeRow
        {
            public string Name { get; set; }

            public bool ShowInDiscoveryDocument { get; set; }

            public ScopeModel Model { get; set; }

            public static ScopeRow FromScope(Scope scope)
            {
                return new ScopeRow()
                {
                    Name = scope.Name,
                    ShowInDiscoveryDocument = scope.ShowInDiscoveryDocument,
                    Model = new ScopeModel
                    {
                        Enabled = scope.Enabled,
                        Name = scope.Name,
                        DisplayName = scope.DisplayName,
                        Description = scope.Description,
                        Required = scope.Required,
                        Emphasize = scope.Emphasize,
                        Type = scope.Type,
                        Claims = scope.Claims,
                        IncludeAllClaimsForUser = scope.IncludeAllClaimsForUser,
                        ClaimsRule = scope.ClaimsRule
                    }
                };
            }

            public static ScopeRow FromReader(DbDataReader reader)
            {
                int nameOrdinal = reader.GetOrdinal("name");
                int isPublicOrdinal = reader.GetOrdinal("is_public");
                int modelOrdinal = reader.GetOrdinal("model");

                var row = new ScopeRow
                {
                    Name = reader.GetString(nameOrdinal),
                    ShowInDiscoveryDocument = reader.GetBoolean(isPublicOrdinal),
                    Model = JsonConvert.DeserializeObject<ScopeModel>(reader.GetString(modelOrdinal))
                };

                return row;
            }

            public Scope ToScope()
            {
                return new Scope
                {
                    Enabled = this.Model.Enabled,
                    Name = this.Model.Name,
                    DisplayName = this.Model.DisplayName,
                    Description = this.Model.Description,
                    Required = this.Model.Required,
                    Emphasize = this.Model.Emphasize,
                    Type = this.Model.Type,
                    Claims = this.Model.Claims,
                    IncludeAllClaimsForUser = this.Model.IncludeAllClaimsForUser,
                    ClaimsRule = this.Model.ClaimsRule
                };
            }

            internal class ScopeModel
            {
                public bool Enabled { get; set; }

                public string Name { get; set; }

                public string DisplayName { get; set; }

                public string Description { get; set; }

                public bool Required { get; set; }

                public bool Emphasize { get; set; }

                public ScopeType Type { get; set; }

                public List<ScopeClaim> Claims { get; set; }

                public bool IncludeAllClaimsForUser { get; set; }

                public string ClaimsRule { get; set; }
            }
        }
    }
}