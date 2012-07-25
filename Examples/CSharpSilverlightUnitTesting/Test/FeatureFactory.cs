namespace CSharpSilverlightUnitTesting
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using TickSpec;

    public class FeatureFactory
    {
        public static IEnumerable<Feature> GetFeatures(Assembly assembly)
        {
            var definitions = new StepDefinitions(assembly);

            var sources =
                assembly
                .GetManifestResourceNames()
                .Where(name => name.EndsWith(".txt") || name.EndsWith(".feature"));

            var namespaces =
                assembly
                .GetTypes()
                .Select(t => t.Namespace)
                .Distinct()
                .OrderByDescending(s => s.Length);

            foreach (var source in sources)
            {
                var stream = assembly.GetManifestResourceStream(source);
                var ns = namespaces.FirstOrDefault(s => source.StartsWith(s + "."));
                var filename = ns != null ? source.Substring(ns.Length + 1) : source;
                var feature = definitions.GenerateFeature(filename, stream);
                yield return feature;
            }
        }
    }
}
