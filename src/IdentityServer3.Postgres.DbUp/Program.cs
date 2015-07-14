namespace IdentityServer3.Postgres.DbUp
{
    using System;
    using System.Configuration;
    using System.Linq;
    using System.Reflection;

    using global::DbUp;

    class Program
    {
        static int Main(string[] args)
        {
            var connectionString = args.FirstOrDefault()
                ?? ConfigurationManager.ConnectionStrings["IdentityServerDb"].ConnectionString;

            var upgrader =
                DeployChanges.To
                    .PostgresqlDatabase(connectionString)
                    .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly())
                    .LogToConsole()
                    .Build();

            var result = upgrader.PerformUpgrade();

            if (!result.Successful)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(result.Error);
                Console.ResetColor();
                return -1;
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Success!");
            Console.ResetColor();
            return 0;
        }
    }
}
