using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace TickSpec.NUnit
{
    [TestFixture]
    public abstract class FeatureFixture
    {
        private readonly string _source;
        private readonly Assembly _assembly;
        private readonly StepDefinitions _definitions;

        public FeatureFixture(string source)
        {
            _source = source;
            _assembly = typeof(FeatureFixture).Assembly;
            _definitions = new StepDefinitions(_assembly);
        }

        [Test, TestCaseSource("Scenarios")]
        public void TestScenario(Scenario scenario)
        {
            if (scenario.Tags.Contains("ignore"))
                throw new IgnoreException("Ignored: " + scenario.Name);
            scenario.Action();
        }

        public IEnumerable<Scenario> Scenarios
        {
            get
            {
                var resourceName = 
                    _assembly
                        .GetManifestResourceNames()
                        .First(name => name.EndsWith(_source));
                var stream = 
                    _assembly
                        .GetManifestResourceStream(resourceName);
                return 
                    _definitions
                        .GenerateScenarios(stream);
            }
        }
    }
}