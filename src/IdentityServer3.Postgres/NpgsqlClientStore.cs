namespace IdentityServer3.Postgres
{
    using System;
    using System.Threading.Tasks;

    using IdentityServer3.Postgres.Converters;

    using Newtonsoft.Json;

    using Npgsql;

    using Thinktecture.IdentityServer.Core.Models;
    using Thinktecture.IdentityServer.Core.Services;

    public class NpgsqlClientStore : IClientStore, IDisposable
    {
        private readonly NpgsqlConnection _conn;
        private readonly JsonSerializer _serializer;
        private readonly string _schema;

        private readonly string _findQuery;
        private readonly string _insertQuery;

        public NpgsqlClientStore(NpgsqlConnection conn)
            : this(conn, "public")
        {
        }

        public NpgsqlClientStore(NpgsqlConnection conn, string schema)
        {
            Preconditions.IsNotNull(conn, nameof(conn));
            Preconditions.IsShortString(schema, nameof(schema));

            _conn = conn;
            _schema = schema;
            _serializer = JsonSerializerFactory.Create();
            _serializer.Converters.Add(new ClaimConverter());

            _findQuery = "SELECT client_id, model " +
                         $"FROM {_schema}.clients " +
                         "WHERE client_id = @client";
            _insertQuery = $"INSERT INTO {_schema}.clients(client_id, model) " +
                           "VALUES(@client, @model);";
        }

        public Task<Client> FindClientByIdAsync(string clientId)
        {
            Preconditions.IsShortString(clientId, nameof(clientId));

            return _conn.ExecuteCommand(_findQuery,
                async cmd =>
                {
                    cmd.Parameters.AddWithValue("client", clientId);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            int modelOrdinal = reader.GetOrdinal("model");

                            return _serializer.Deserialize<Client>(reader.GetString(modelOrdinal));
                        }
                        else
                        {
                            return null;
                        }
                    }
                });

        }

        public Task AddClientAsync(Client client)
        {
            Preconditions.IsNotNull(client, nameof(client));

            return _conn.ExecuteCommand(_insertQuery,
                async cmd =>
                {
                    string model = _serializer.Serialize(client);
                    Console.WriteLine(model);
                    cmd.Parameters.AddWithValue("client", client.ClientId);
                    cmd.Parameters.AddWithValue("model", model);

                    await cmd.ExecuteNonQueryAsync();

                    return true;
                });
        }

        public void InitializeTable()
        {
            string query = $"CREATE TABLE IF NOT EXISTS {_schema}.clients (" +
                           "client_id character varying(255) NOT NULL," +
                           "model jsonb NOT NULL," +
                           "CONSTRAINT pk_clients_clientid PRIMARY KEY(client_id)" +
                           ") WITH (OIDS = FALSE);";

            _conn.ExecuteCommand(query,
                async cmd =>
                {
                    await cmd.ExecuteNonQueryAsync();

                    return 0;
                }).GetAwaiter().GetResult();
        }

        public void Dispose()
        {
            ((IDisposable) _conn).Dispose();
        }
    }
}
