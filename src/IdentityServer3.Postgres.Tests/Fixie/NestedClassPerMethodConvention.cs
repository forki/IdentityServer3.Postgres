namespace IdentityServer3.Postgres.Tests.Fixie
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    using global::Fixie;

    using Ploeh.AutoFixture.Kernel;

    using Fixture = Ploeh.AutoFixture.Fixture;

    public class NestedClassPerMethodConvention : Convention
    {
        public NestedClassPerMethodConvention()
        {
            // x.DeclaringType is not null since x.IsNested
            // IsNested is actually a check for x.DeclaringType != null.
            Classes
                // ReSharper disable once PossibleNullReferenceException
                .Where(x => x.IsNested && x.DeclaringType.Name.EndsWith("Tests"))
                .Where(t => t.GetConstructors().All(ci => ci.GetParameters().Length == 0));

            Methods.Where(mi => mi.IsPublic && (mi.IsVoid() || mi.IsAsync()));

            Parameters.Add(FillFromFixture);
        }

        private IEnumerable<object[]> FillFromFixture(MethodInfo method)
        {
            var fixture = new Fixture();

            fixture.Customize(new StructureMapCustomization());

            yield return GetParameterData(method.GetParameters(), fixture);
        }

        private object[] GetParameterData(ParameterInfo[] parameters, Fixture fixture)
        {
            return parameters
                .Select(p => new SpecimenContext(fixture).Resolve(p))
                .ToArray();
        }
    }
}
