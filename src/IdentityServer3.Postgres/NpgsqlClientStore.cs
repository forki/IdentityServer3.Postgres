namespace IdentityServer3.Postgres
{
    using System.Collections.Generic;
    using System.Data.Common;
    using System.Linq;
    using System.Security.Claims;
    using System.Threading.Tasks;

    using IdentityServer3.Postgres.Converters;

    using Newtonsoft.Json;

    using Npgsql;

    using Thinktecture.IdentityServer.Core.Models;
    using Thinktecture.IdentityServer.Core.Services;

    public class NpgsqlClientStore : IClientStore
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
                            var row = ClientRow.FromReader(reader, _serializer);

                            return row.ToClient();
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
                    var row = ClientRow.FromClient(client);
                    cmd.Parameters.AddWithValue("client", row.ClientId);
                    cmd.Parameters.AddWithValue("model", _serializer.Serialize(row.Model));

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

        internal class ClientRow
        {
            public string ClientId { get; set; }

            public ClientModel Model { get; set; }

            public static ClientRow FromReader(DbDataReader reader, JsonSerializer serializer)
            {
                int idOrdinal = reader.GetOrdinal("client_id");
                int modelOrdinal = reader.GetOrdinal("model");

                var serializedModel = reader.GetString(modelOrdinal);

                var model = serializer.Deserialize<ClientModel>(serializedModel);

                var row = new ClientRow
                {
                    ClientId = reader.GetString(idOrdinal),
                    Model = model,

                };

                return row;
            }

            public static ClientRow FromClient(Client client)
            {
                var row = new ClientRow()
                {
                    ClientId = client.ClientId,
                    Model = ClientModel.FromClient(client)
                };

                return row;
            }

            public Client ToClient()
            {
                var client = this.Model.ToClient();
                client.ClientId = this.ClientId;

                return client;
            }

            internal class ClientModel
            {
                public static ClientModel FromClient(Client client)
                {
                    var model = new ClientModel
                    {
                        Enabled = client.Enabled,
                        ClientSecrets = client.ClientSecrets,
                        ClientName = client.ClientName,
                        ClientUri = client.ClientUri,
                        LogoUri = client.LogoUri,
                        RequireConsent = client.RequireConsent,
                        AllowRememberConsent = client.AllowRememberConsent,
                        Flow = client.Flow,
                        AllowClientCredentialsOnly = client.AllowClientCredentialsOnly,
                        Claims = client.Claims,
                        AccessTokenType = client.AccessTokenType,
                        AbsoluteRefreshTokenLifetime = client.AbsoluteRefreshTokenLifetime,
                        AccessTokenLifetime = client.AccessTokenLifetime,
                        AllowedCorsOrigins = client.AllowedCorsOrigins,
                        AlwaysSendClientClaims = client.AlwaysSendClientClaims,
                        AuthorizationCodeLifetime = client.AuthorizationCodeLifetime,
                        CustomGrantTypeRestrictions = client.CustomGrantTypeRestrictions,
                        EnableLocalLogin = client.EnableLocalLogin,
                        IdentityProviderRestrictions = client.IdentityProviderRestrictions,
                        IdentityTokenLifetime = client.IdentityTokenLifetime,
                        IncludeJwtId = client.IncludeJwtId,
                        PostLogoutRedirectUris = client.PostLogoutRedirectUris,
                        PrefixClientClaims = client.PrefixClientClaims,
                        RedirectUris = client.RedirectUris,
                        RefreshTokenExpiration = client.RefreshTokenExpiration,
                        RefreshTokenUsage = client.RefreshTokenUsage,
                        ScopeRestrictions = client.ScopeRestrictions,
                        SlidingRefreshTokenLifetime = client.SlidingRefreshTokenLifetime,
                        UpdateAccessTokenClaimsOnRefresh = client.UpdateAccessTokenClaimsOnRefresh
                    };

                    return model;
                }

                public Client ToClient()
                {
                    var client = new Client()
                    {
                        RedirectUris = this.RedirectUris,
                        RefreshTokenUsage = this.RefreshTokenUsage,
                        ScopeRestrictions = this.ScopeRestrictions,
                        SlidingRefreshTokenLifetime = this.SlidingRefreshTokenLifetime,
                        UpdateAccessTokenClaimsOnRefresh = this.UpdateAccessTokenClaimsOnRefresh,
                        AbsoluteRefreshTokenLifetime = this.AbsoluteRefreshTokenLifetime,
                        ClientId = null,
                        ClientSecrets = this.ClientSecrets,
                        Flow = this.Flow,
                        Claims = this.Claims,
                        AccessTokenType = this.AccessTokenType,
                        ClientUri = this.ClientUri,
                        ClientName = this.ClientName,
                        RequireConsent = this.RequireConsent,
                        LogoUri = this.LogoUri,
                        Enabled = this.Enabled,
                        RefreshTokenExpiration = this.RefreshTokenExpiration,
                        IdentityTokenLifetime = this.IdentityTokenLifetime,
                        EnableLocalLogin = this.EnableLocalLogin,
                        IncludeJwtId = this.IncludeJwtId,
                        AlwaysSendClientClaims = this.AlwaysSendClientClaims,
                        AllowedCorsOrigins = this.AllowedCorsOrigins,
                        IdentityProviderRestrictions = this.IdentityProviderRestrictions,
                        CustomGrantTypeRestrictions = this.CustomGrantTypeRestrictions,
                        AccessTokenLifetime = this.AccessTokenLifetime,
                        PostLogoutRedirectUris = this.PostLogoutRedirectUris,
                        PrefixClientClaims = this.PrefixClientClaims,
                        AuthorizationCodeLifetime = this.AuthorizationCodeLifetime,
                        AllowClientCredentialsOnly = this.AllowClientCredentialsOnly,
                        AllowRememberConsent = this.AllowRememberConsent
                    };

                    return client;
                }

                /// <summary>
                /// Specifies if client is enabled (defaults to true)
                /// 
                /// </summary>
                public bool Enabled { get; set; }

                /// <summary>
                /// Client secrets - only relevant for flows that require a secret
                /// 
                /// </summary>
                public List<ClientSecret> ClientSecrets { get; set; }

                /// <summary>
                /// Client display name (used for logging and consent screen)
                /// 
                /// </summary>
                public string ClientName { get; set; }

                /// <summary>
                /// URI to further information about client (used on consent screen)
                /// 
                /// </summary>
                public string ClientUri { get; set; }

                /// <summary>
                /// URI to client logo (used on consent screen)
                /// 
                /// </summary>
                public string LogoUri { get; set; }

                /// <summary>
                /// Specifies whether a consent screen is required (defaults to true)
                /// 
                /// </summary>
                public bool RequireConsent { get; set; }

                /// <summary>
                /// Specifies whether user can choose to store consent decisions (defaults to true)
                /// 
                /// </summary>
                public bool AllowRememberConsent { get; set; }

                /// <summary>
                /// Specifies allowed flow for client (either AuthorizationCode, Implicit, Hybrid, ResourceOwner, ClientCredentials or Custom). Defaults to Implicit.
                /// 
                /// </summary>
                public Flows Flow { get; set; }

                /// <summary>
                /// Gets or sets a value indicating whether this client is allowed to request token using client credentials only.
                ///             This is e.g. useful when you want a client to be able to use both a user-centric flow like implicit and additionally client credentials flow
                /// 
                /// </summary>
                /// 
                /// <value>
                /// <c>true</c> if client credentials flow is allowed; otherwise, <c>false</c>.
                /// 
                /// </value>
                public bool AllowClientCredentialsOnly { get; set; }

                /// <summary>
                /// Specifies allowed URIs to return tokens or authorization codes to
                /// 
                /// </summary>
                public List<string> RedirectUris { get; set; }

                /// <summary>
                /// Specifies allowed URIs to redirect to after logout
                /// 
                /// </summary>
                public List<string> PostLogoutRedirectUris { get; set; }

                /// <summary>
                /// Specifies the scopes that the client is allowed to request. If empty, the client can request all scopes (defaults to empty)
                /// 
                /// </summary>
                public List<string> ScopeRestrictions { get; set; }

                /// <summary>
                /// Lifetime of identity token in seconds (defaults to 300 seconds / 5 minutes)
                /// 
                /// </summary>
                public int IdentityTokenLifetime { get; set; }

                /// <summary>
                /// Lifetime of access token in seconds (defaults to 3600 seconds / 1 hour)
                /// 
                /// </summary>
                public int AccessTokenLifetime { get; set; }

                /// <summary>
                /// Lifetime of authorization code in seconds (defaults to 300 seconds / 5 minutes)
                /// 
                /// </summary>
                public int AuthorizationCodeLifetime { get; set; }

                /// <summary>
                /// Maximum lifetime of a refresh token in seconds. Defaults to 2592000 seconds / 30 days
                /// 
                /// </summary>
                public int AbsoluteRefreshTokenLifetime { get; set; }

                /// <summary>
                /// Sliding lifetime of a refresh token in seconds. Defaults to 1296000 seconds / 15 days
                /// 
                /// </summary>
                public int SlidingRefreshTokenLifetime { get; set; }

                /// ///
                /// <summary>
                /// ReUse: the refresh token handle will stay the same when refreshing tokens
                ///             OneTime: the refresh token handle will be updated when refreshing tokens
                /// 
                /// </summary>
                public TokenUsage RefreshTokenUsage { get; set; }

                /// <summary>
                /// Gets or sets a value indicating whether the access token (and its claims) should be updated on a refresh token request.
                /// 
                /// </summary>
                /// 
                /// <value>
                /// <c>true</c> if the token should be updated; otherwise, <c>false</c>.
                /// 
                /// </value>
                public bool UpdateAccessTokenClaimsOnRefresh { get; set; }

                /// <summary>
                /// Absolute: the refresh token will expire on a fixed point in time (specified by the AbsoluteRefreshTokenLifetime)
                ///             Sliding: when refreshing the token, the lifetime of the refresh token will be renewed (by the amount specified in SlidingRefreshTokenLifetime). The lifetime will not exceed
                /// 
                /// </summary>
                public TokenExpiration RefreshTokenExpiration { get; set; }

                /// <summary>
                /// Specifies whether the access token is a reference token or a self contained JWT token (defaults to Jwt).
                /// 
                /// </summary>
                public AccessTokenType AccessTokenType { get; set; }

                /// <summary>
                /// Gets or sets a value indicating whether the local login is allowed for this client. Defaults to <c>true</c>.
                /// 
                /// </summary>
                /// 
                /// <value>
                /// <c>true</c> if local logins are enabled; otherwise, <c>false</c>.
                /// 
                /// </value>
                public bool EnableLocalLogin { get; set; }

                /// <summary>
                /// Specifies which external IdPs can be used with this client (if list is empty all IdPs are allowed). Defaults to empty.
                /// 
                /// </summary>
                public List<string> IdentityProviderRestrictions { get; set; }

                /// <summary>
                /// Gets or sets a value indicating whether JWT access tokens should include an identifier
                /// 
                /// </summary>
                /// 
                /// <value>
                /// <c>true</c> to add an id; otherwise, <c>false</c>.
                /// 
                /// </value>
                public bool IncludeJwtId { get; set; }

                /// <summary>
                /// Allows settings claims for the client (will be included in the access token).
                /// 
                /// </summary>
                /// 
                /// <value>
                /// The claims.
                /// 
                /// </value>
                public List<Claim> Claims { get; set; }

                /// <summary>
                /// Gets or sets a value indicating whether client claims should be always included in the access tokens - or only for client credentials flow.
                /// 
                /// </summary>
                /// 
                /// <value>
                /// <c>true</c> if claims should always be sent; otherwise, <c>false</c>.
                /// 
                /// </value>
                public bool AlwaysSendClientClaims { get; set; }

                /// <summary>
                /// Gets or sets a value indicating whether all client claims should be prefixed.
                /// 
                /// </summary>
                /// 
                /// <value>
                /// <c>true</c> if client claims should be prefixed; otherwise, <c>false</c>.
                /// 
                /// </value>
                public bool PrefixClientClaims { get; set; }

                /// <summary>
                /// Gets or sets a list of allowed custom grant types when Flow is set to Custom. If the list is empty, all custom grant types are allowed.
                /// 
                /// </summary>
                /// 
                /// <value>
                /// The custom grant restrictions.
                /// 
                /// </value>
                public List<string> CustomGrantTypeRestrictions { get; set; }

                /// <summary>
                /// Gets or sets the allowed CORS origins for JavaScript clients.
                /// 
                /// </summary>
                /// 
                /// <value>
                /// The allowed CORS origins.
                /// 
                /// </value>
                public List<string> AllowedCorsOrigins { get; set; }

            }
        }
    }
}
