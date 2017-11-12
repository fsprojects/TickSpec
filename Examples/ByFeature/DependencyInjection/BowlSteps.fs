namespace Dependency

open NUnit.Framework
open TickSpec

type BowlSteps(bowl: DogBowl) =
    [<Then>]
    member __.``The bowl contains (.*)g of food`` (amount:int) =
        Assert.AreEqual(amount, bowl.FoodAmount)