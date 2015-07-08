namespace IdentityServer3.Postgres.Tests.Fixie
{
    using Ploeh.AutoFixture;

    public class StructureMapCustomization : ICustomization
    {
        public void Customize(IFixture fixture)
        {
            var contextFixture = new StructureMapFixture();

            fixture.Register(() => contextFixture);
            fixture.Customizations.Add(new ContainerBuilder(contextFixture.Container));
        }
    }
}
