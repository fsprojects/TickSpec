namespace Dependency

open TickSpec
open NUnit.Framework

type BowlSteps(bowl: DogBowl) =
    [<Then>]
    member this.``The bowl contains (.*)g of food`` (amount:int) =
        Assert.AreEqual(amount, bowl.FoodAmount)