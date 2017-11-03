module Fixtures

open TickSpec.NUnit

type ShoppingFeature () = inherit FeatureFixture("Shopping.feature")

type DependencyFeature () = inherit FeatureFixture("Dependency.feature")

type StockFeature () = inherit FeatureFixture("Stock.feature")