namespace IdentityServer3.Postgres
{
    using System;
    using System.Data;
    using System.Threading.Tasks;

    using Npgsql;

    internal static class TransactionHelper
    {
        public static async Task<T> ExecuteCommand<T>(this NpgsqlConnection conn,
            string commandText, Func<NpgsqlCommand, Task<T>> func)
        {
            T result;

            if (conn.State != ConnectionState.Open)
            {
                await conn.OpenAsync();
            }

            using (var tx = conn.BeginTransaction())
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = tx;
                    cmd.CommandText = commandText;

                    result = await func(cmd);
                }

                tx.Commit();
            }

            return result;
        }
    }
}
