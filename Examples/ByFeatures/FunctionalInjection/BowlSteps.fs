module BowlSteps

open TickSpec
open NUnit.Framework
open Domain

let [<Then>] ``The bowl contains (.*)g of food`` (amount:int) (bowl:DogBowl) =
        Assert.AreEqual(amount, bowl.FoodAmount)