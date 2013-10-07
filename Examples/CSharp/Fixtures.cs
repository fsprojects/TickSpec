using TickSpec.NUnit;

public class StockFixture : FeatureFixture
{
    public StockFixture() : base("Feature.txt") { }
}

// Adding a new feature
// (1) Create a feature file and set it as an Embedded Resource
// (2) Create a fixture class that derives from FeatureFixture as above 