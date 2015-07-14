namespace IdentityServer3.Postgres
{
    public class NpgsqlSchema
    {
        public readonly string Schema;

        public NpgsqlSchema(string schema)
        {
            Schema = schema;
        }

        public static implicit operator NpgsqlSchema(string x)
        {
            return new NpgsqlSchema(x);
        }

        public static implicit operator string (NpgsqlSchema x)
        {
            return x.Schema;
        }
    }
}