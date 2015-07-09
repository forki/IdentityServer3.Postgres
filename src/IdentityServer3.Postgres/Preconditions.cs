namespace IdentityServer3.Postgres
{
    using System;
    using System.Diagnostics.Contracts;

    internal static class Preconditions
    {
        public static void IsNotNull<T>(T value, string name) where T : class
        {
            if (ReferenceEquals(value, null))
            {
                throw new ArgumentNullException(name, $"{name} must not be null.");
            }

            Contract.EndContractBlock();
        }

        public static void AreNotEqual<T>(T actual, T notExpected, string name)
        {
            if (actual.Equals(notExpected))
            {
                throw new ArgumentException($"{name} must not be equal to '{notExpected}'.");
            }

            Contract.EndContractBlock();
        }

        public static void IsNotBlank(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException($"{name} should not be blank.");
            }

            Contract.EndContractBlock();
        }

        /// <summary>
        ///     Asserts that the given value is a string that does not exceed 'maxLength' characters.
        /// </summary>
        /// <param name="value">The string to check.</param>
        /// <param name="name">The name of the parameter.</param>
        /// <param name="maxLength">The maximum length to check, defaults to 255.</param>
        public static void IsShortString(string value, string name, int maxLength = 255)
        {
            IsNotBlank(value, name);

            if (value.Length > maxLength)
            {
                throw new ArgumentException($"{name} must not exceed {maxLength} characters.");
            }

            Contract.EndContractBlock();
        }
    }
}
