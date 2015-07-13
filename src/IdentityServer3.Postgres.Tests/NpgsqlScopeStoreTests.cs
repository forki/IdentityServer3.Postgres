namespace IdentityServer3.Postgres.Tests
{
    using System.Linq;
    using System.Threading.Tasks;

    using Shouldly;

    using Thinktecture.IdentityServer.Core.Models;

    public class NpgsqlScopeStoreTests
    {
        public class Test
        {
            public async Task Insert(NpgsqlScopeStore store, Scope scope)
            {
                await store.SaveScopeAsync(scope);
            }
        }

        public class TheFindScopesAsyncMethod
        {
            public async Task CanFindASingleScope(NpgsqlScopeStore store, Scope scope)
            {
                // Given
                await store.SaveScopeAsync(scope);

                // When
                var scopes = await store.FindScopesAsync(new[] { scope.Name });

                // Then
                scopes.Count().ShouldBe(1);

            }
        }
    }
}