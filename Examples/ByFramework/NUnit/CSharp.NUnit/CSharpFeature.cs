using TickSpec.NUnit;

namespace ByFramework.NUnit.CSharp
{
    public class Stock : FeatureFixture
    {
        public Stock() : base("Stock.feature") { }
    }
}
// Adding a new feature
// (1) Create a feature file and set it as an Embedded Resource
// (2) Create a fixture class that derives from FeatureFixture as above 