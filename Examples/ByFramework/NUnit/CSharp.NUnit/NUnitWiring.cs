using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace TickSpec.NUnit
{
    [TestFixture]
    public class FeatureFixture
    {

        [Test, TestCaseSource("Source")]
        public void Test(Scenario scenario)
        {
            if (scenario.Tags.Contains("ignore"))
                throw new IgnoreException("Ignored: " + scenario.Name);
            scenario.Action();
        }

        public static IEnumerable<TestCaseData> Source
        {
            get
            {
                var assembly = typeof(FeatureFixture).Assembly;
                var definitions = new StepDefinitions(assembly);
                var resourceName = 
                    assembly
                        .GetManifestResourceNames()
                        .First(name => name.EndsWith(".feature"));
                var stream = 
                    assembly
                        .GetManifestResourceStream(resourceName);
                var feature = definitions.GenerateFeature(resourceName, stream);
                var scenarios = feature.Scenarios;
                return 
                    scenarios.Select(s => (new TestCaseData(s)).SetName(s.Name).SetProperty("Feature", feature.Name.Substring(9)));
            }
        }
    }
}