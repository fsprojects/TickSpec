module Fixtures

open TickSpec.NUnit

type ShoppingFeature () = inherit FeatureFixture("Shopping.feature")

type DependencyFeature () = inherit FeatureFixture("Dependency.feature")