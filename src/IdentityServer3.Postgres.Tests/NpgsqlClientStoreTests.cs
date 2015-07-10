namespace IdentityServer3.Postgres.Tests
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Claims;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;

    using Ploeh.AutoFixture;

    using Shouldly;

    using Thinktecture.IdentityServer.Core.Models;

    public class NpgsqlClientStoreTests
    {
        public class TheAddClientAsyncMethod
        {
            public async Task CanStoreSimpleClient(NpgsqlClientStore store, string clientId)
            {
                // Given
                var client = new Client()
                {
                    ClientId = clientId
                };

                // When
                await store.AddClientAsync(client);

                // Then
                var fromDb = await store.FindClientByIdAsync(clientId);
                fromDb.ShouldNotBe(null);

                fromDb.ClientId.ShouldBe(client.ClientId);
            }

            public async Task CanStoreComplexClient(NpgsqlClientStore store, Fixture fixture, string clientId)
            {
                // Given
                var client = new Client
                {
                    ClientId = clientId,
                    ClientSecrets = fixture.Create<List<ClientSecret>>(),
                    Flow = Flows.AuthorizationCode,
                    Claims = new List<Claim> { new Claim(fixture.Create("type"), fixture.Create("value"))},
                    AccessTokenType = AccessTokenType.Jwt,
                    ClientUri = fixture.Create<string>(),
                    ClientName = fixture.Create<string>(),
                    RequireConsent = false,
                    ScopeRestrictions = fixture.Create<List<string>>(),
                    LogoUri = fixture.Create<string>(),
                    Enabled = true,
                };

                // When
                await store.AddClientAsync(client);

                // Then
                var fromDb = await store.FindClientByIdAsync(clientId);
                fromDb.ShouldNotBe(null);

                fromDb.ClientId.ShouldBe(clientId);
                fromDb.ClientSecrets.Count.ShouldBe(client.ClientSecrets.Count);
                fromDb.Flow.ShouldBe(client.Flow);
                
            }
        }
    }
}