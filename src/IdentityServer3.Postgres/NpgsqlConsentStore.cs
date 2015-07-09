namespace IdentityServer3.Postgres
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Threading.Tasks;

    using Npgsql;

    using Thinktecture.IdentityServer.Core.Models;
    using Thinktecture.IdentityServer.Core.Services;

    public class NpgsqlConsentStore : IConsentStore
    {
        private readonly NpgsqlConnection _conn;
        private readonly string _schema;
       
        // Queries
        private readonly string _loadAllQuery;
        private readonly string _loadQuery;
        private readonly string _revokeQuery;
        private readonly string _rowExistsQuery;
        private readonly string _insertQuery;
        private readonly string _updateQuery;

        public NpgsqlConsentStore(NpgsqlConnection conn)
            : this(conn, "public")
        {
        }

        public NpgsqlConsentStore(NpgsqlConnection conn, string schema)
        {
            if (conn == null)
            {
                throw new ArgumentNullException(nameof(conn));
            }

            if (string.IsNullOrWhiteSpace(schema))
            {
                throw new ArgumentException("schema is null or whitespace", nameof(schema));
            }

            _conn = conn;
            _schema = schema;

            _loadAllQuery = "SELECT subject, client_id, scopes " +
                            $"FROM {_schema}.consents " +
                            "WHERE subject = @subject";

            _revokeQuery = $"DELETE FROM {_schema}.consents " +
                           "WHERE subject = @subject AND client_id = @client";

            _loadQuery = "SELECT subject, client_id, scopes " +
                         $"FROM {_schema}.consents " +
                         "WHERE subject = @subject AND client_id = @client";

            _rowExistsQuery = "SELECT COUNT(subject) " +
                              $"FROM {_schema}.consents " +
                              "WHERE subject = @subject AND client_id = @client";

            _insertQuery = $"INSERT INTO {_schema}.consents(subject, client_id, scopes) " +
                           "VALUES (@subject, @client, @scopes)";

            _updateQuery = $"UPDATE {_schema}.consents " +
                           "SET scopes = @scopes " +
                           "WHERE subject = @subject AND client_id = @client";
        }

        public Task<IEnumerable<Consent>> LoadAllAsync(string subject)
        {
            return _conn.ExecuteCommand(_loadAllQuery, async cmd =>
                {
                    cmd.Parameters.AddWithValue("subject", subject);

                    var consentList = new List<Consent>();

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        var hasMoreRowsTask = reader.ReadAsync();
                        while (await hasMoreRowsTask)
                        {
                            var row = ConsentRow.Read(reader);

                            hasMoreRowsTask = reader.ReadAsync();

                            consentList.Add(row.ToConsent());
                        }
                    }

                    return (IEnumerable<Consent>)consentList;
                });
        }

        public Task RevokeAsync(string subject, string client)
        {
            return _conn.ExecuteCommand(_revokeQuery, async cmd =>
                {
                    cmd.Parameters.AddWithValue("subject", subject);
                    cmd.Parameters.AddWithValue("client", client);

                    int rowsAffected = await cmd.ExecuteNonQueryAsync();

                    return rowsAffected;
                });
        }

        public Task<Consent> LoadAsync(string subject, string client)
        {
            return _conn.ExecuteCommand(_loadQuery, async cmd =>
                {
                    cmd.Parameters.AddWithValue("@subject", subject);
                    cmd.Parameters.AddWithValue("@client", client);

                    Consent consent;

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            var row = ConsentRow.Read(reader);

                            consent = row.ToConsent();
                        }
                        else
                        {
                            consent = null;
                        }
                    }

                    return consent;
                });
        }

        public async Task UpdateAsync(Consent consent)
        {
            var consentExistsTask = ConsentExistsInTable(consent);
            var row = ConsentRow.Convert(consent);

            if (await consentExistsTask)
            {
                // Update
                await _conn.ExecuteCommand(_updateQuery, async cmd =>
                    {
                        cmd.Parameters.AddWithValue("subject", row.Subject);
                        cmd.Parameters.AddWithValue("client", row.ClientId);
                        cmd.Parameters.AddWithValue("scopes", row.ScopesAsString);

                        int rowsAffected = await cmd.ExecuteNonQueryAsync();

                        return rowsAffected;
                    });
            }
            else
            {
                // Insert
                await _conn.ExecuteCommand(_insertQuery, async cmd =>
                    {
                        cmd.Parameters.AddWithValue("subject", row.Subject);
                        cmd.Parameters.AddWithValue("client", row.ClientId);
                        cmd.Parameters.AddWithValue("scopes", row.ScopesAsString);

                        int rowsAffected = await cmd.ExecuteNonQueryAsync();

                        return rowsAffected;
                    });
            }
        }

        public void InitializeTable()
        {
            string query = $"CREATE TABLE IF NOT EXISTS {_schema}.consents (" +
                           "subject character varying(255) NOT NULL," +
                           "client_id character varying(255) NOT NULL," +
                           "scopes character varying(2000) NOT NULL," +
                           "CONSTRAINT pk_consents_subject_client PRIMARY KEY(subject, client_id)" +
                           ") WITH (OIDS = FALSE); ";

            _conn.ExecuteCommand(query,
                cmd =>
                {
                    cmd.ExecuteNonQuery();

                    return Task.FromResult(true);
                }).GetAwaiter()
                  .GetResult();
        }

        private Task<bool> ConsentExistsInTable(Consent consent)
        {
            return _conn.ExecuteCommand(_rowExistsQuery, async cmd =>
                {
                    cmd.Parameters.AddWithValue("subject", consent.Subject);
                    cmd.Parameters.AddWithValue("client", consent.ClientId);

                    var rowCount = (long)await cmd.ExecuteScalarAsync();

                    return rowCount > 0;
                });
        }

        private class ConsentRow
        {
            public string Subject { get; set; }

            public string ClientId { get; set; }

            public string ScopesAsString { get; set; }

            public static ConsentRow Read(IDataReader reader)
            {
                int subjectOrdinal = reader.GetOrdinal("subject");
                int clientIdOrdinal = reader.GetOrdinal("client_id");
                int scopesOrdinal = reader.GetOrdinal("scopes");

                return new ConsentRow
                {
                    Subject = reader.GetString(subjectOrdinal),
                    ClientId = reader.GetString(clientIdOrdinal),
                    ScopesAsString = reader.GetString(scopesOrdinal)
                };
            }

            public static ConsentRow Convert(Consent consent)
            {
                return new ConsentRow
                {
                    ClientId = consent.ClientId,
                    Subject = consent.Subject,
                    ScopesAsString = SerializeScopes(consent.Scopes)
                };
            }

            private static string SerializeScopes(IEnumerable<string> scopes)
            {
                if (scopes == null || !scopes.Any())
                {
                    return string.Empty;
                }

                return scopes.Aggregate((s1, s2) => $"{s1},{s2}");
            }

            private static IEnumerable<string> DeserializeScopes(string scopes)
            {
                if (scopes == null)
                {
                    return new string[0];
                }

                return scopes.Split(',');
            }

            public Consent ToConsent()
            {
                return new Consent
                {
                    ClientId = ClientId,
                    Subject = Subject,
                    Scopes = DeserializeScopes(ScopesAsString)
                };
            }
        }
    }
}
