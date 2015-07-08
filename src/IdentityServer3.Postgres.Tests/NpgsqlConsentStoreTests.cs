namespace IdentityServer3.Postgres.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Shouldly;

    using Thinktecture.IdentityServer.Core.Models;

    public class NpgsqlConsentStoreTests
    {
        public class TheUpdateAsyncMethod
        {
            public async Task InsertsConsentIfItDoesNotExist(NpgsqlConsentStore store, Consent consent)
            {
                // Given
                var before = await store.LoadAsync(consent.Subject, consent.ClientId);
                before.ShouldBe(null);

                // When
                await store.UpdateAsync(consent);

                // Then
                var fromDb = await store.LoadAsync(consent.Subject, consent.ClientId);

                fromDb.ShouldNotBe(null);
                fromDb.ClientId.ShouldBe(consent.ClientId);
                fromDb.Subject.ShouldBe(consent.Subject);
            }

            public async Task UpdatesTheExistingConsentIfItExists(NpgsqlConsentStore store, Consent consent, IEnumerable<string> newScopes)
            {
                // Given
                await store.UpdateAsync(consent);

                // When
                consent.Scopes = newScopes;
                await store.UpdateAsync(consent);

                // Then
                var fromDb = await store.LoadAsync(consent.Subject, consent.ClientId);

                fromDb.ShouldNotBe(null);
                fromDb.ClientId.ShouldBe(consent.ClientId);
                fromDb.Subject.ShouldBe(consent.Subject);
                fromDb.Scopes.All(newScopes.Contains).ShouldBe(true);
            }
        }

        public class TheRevokeAsyncMethod
        {
            public async Task RevokesAGivenConsent(NpgsqlConsentStore store, Consent consent)
            {
                // Given
                await store.UpdateAsync(consent);

                // When
                await store.RevokeAsync(consent.Subject, consent.ClientId);

                // Then
                var fromDb = await store.LoadAsync(consent.Subject, consent.ClientId);

                fromDb.ShouldBe(null);
            }

            public async Task IsIdemPotent(NpgsqlConsentStore store, Consent consent)
            {
                // Given
                await store.UpdateAsync(consent);

                // When
                await store.RevokeAsync(consent.Subject, consent.ClientId);
                await store.RevokeAsync(consent.Subject, consent.ClientId);

                // Then
                var fromDb = await store.LoadAsync(consent.Subject, consent.ClientId);

                fromDb.ShouldBe(null);
            }
        }

        public class TheLoadAllAsyncMethod
        {
            public async Task LoadsAllConsentsForASubject(NpgsqlConsentStore store, 
                                                          Consent consent1, 
                                                          Consent consent2)
            {
                // Given
                consent2.Subject = consent1.Subject;
                await store.UpdateAsync(consent1);
                await store.UpdateAsync(consent2);

                // When
                var fromDb = await store.LoadAllAsync(consent1.Subject);

                // Then
                fromDb.Count().ShouldBe(2);
                fromDb.ShouldContain(x => x.ClientId == consent1.ClientId);
                fromDb.ShouldContain(x => x.ClientId == consent2.ClientId);
                fromDb.ShouldContain(x => x.Scopes.All(y => consent1.Scopes.Contains(y)));
                fromDb.ShouldContain(x => x.Scopes.All(y => consent2.Scopes.Contains(y)));
            }

            public async Task LoadsOnlyTheConsentsGivenForTheSubject(NpgsqlConsentStore store,
                                                                    Consent consent1,
                                                                    Consent consent2)
            {
                // Given
                await store.UpdateAsync(consent1);
                await store.UpdateAsync(consent2);

                // When
                var fromDb = await store.LoadAllAsync(consent1.Subject);

                // Then
                fromDb.Count().ShouldBe(1);
                fromDb.Single().Subject.ShouldBe(consent1.Subject);
            }
        }
    }
}
