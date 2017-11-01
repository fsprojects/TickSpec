module DependencySteps

open TickSpec
open NUnit.Framework

type DependencyFixture () = inherit TickSpec.NUnit.FeatureFixture("Dependency.feature")

type DogSize =
    | Small
    | Medium
    | Large

type DogBowl () =
    member val FoodAmount = 0 with get, set

type Dog (size: DogSize) =
    member this.AmountToEat =
        match size with
        | Small -> 100
        | Medium -> 200
        | Large -> 400

type PersonSteps(instanceProvider: IInstanceProvider, bowl: DogBowl) =
    [<Given>]
    member this.``I have a (.*) dog`` (size) =
        match size with
        | "large" -> instanceProvider.RegisterInstanceAs<Dog>(Dog(Large))
        | "medium" -> instanceProvider.RegisterInstanceAs<Dog>(Dog(Medium))
        | "small" -> instanceProvider.RegisterInstanceAs<Dog>(Dog(Small))
        | _ -> Assert.Fail("Unsupported dog size")

    [<When>]
    member this.``I fill the dog bowl with (.*)g of food`` (amount:int) =
        bowl.FoodAmount <- amount

type DogSteps(bowl: DogBowl, dog: Dog) =
    [<When>]
    member this.``The dog eats the food from bowl`` () =
        bowl.FoodAmount <- bowl.FoodAmount - dog.AmountToEat

type BowlSteps(bowl: DogBowl) =
    [<Then>]
    member this.``The bowl contains (.*)g of food`` (amount:int) =
        Assert.AreEqual(amount, bowl.FoodAmount)