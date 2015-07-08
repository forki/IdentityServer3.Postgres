namespace IdentityServer3.Postgres.Tests.Fixie
{
    using System.Configuration;

    using Npgsql;

    using StructureMap;

    public class StructureMapFixture
    {
        public static IContainer Root = new Container(cfg =>
        {
            cfg.For<NpgsqlConnection>().Use("from_connection_string", ctx =>
                {
                    var cs = ConfigurationManager.ConnectionStrings["TestDb"]
                                                     .ConnectionString;

                    return new NpgsqlConnection(cs);
                })
               .Transient();
        });

        public IContainer Container { get; }

        public StructureMapFixture()
        {
            Container = Root.CreateChildContainer();
        }
    }
}
