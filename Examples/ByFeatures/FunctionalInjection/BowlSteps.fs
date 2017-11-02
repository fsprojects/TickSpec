module Dependency.BowlSteps

open TickSpec
open NUnit.Framework

let [<Then>] ``The bowl contains (.*)g of food`` (amount:int) (bowl:DogBowl) =
    Assert.AreEqual(amount, bowl.FoodAmount)