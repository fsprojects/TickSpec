module Dependency.BowlSteps

open NUnit.Framework
open TickSpec

let [<Then>] ``The bowl contains (.*)g of food`` (amount:int) (bowl:DogBowl) =
    Assert.AreEqual(amount, bowl.FoodAmount)