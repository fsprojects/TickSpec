module DependencySteps

open TickSpec
open NUnit.Framework
open System

type DependencyFixture () = inherit TickSpec.NUnit.FeatureFixture("Dependency.feature")

type DogSize =
    | Small
    | Medium
    | Large

type DogBowl () =
    member val FoodAmount = 0u with get, set

    member this.Eat amount =
        if amount > this.FoodAmount then raise (ArgumentException("The amount of food to eat has to be less or equal to the amount in bowl", "amount"))
        this.FoodAmount <- this.FoodAmount - amount

type Dog (size: DogSize) =
    member this.AmountToEat =
        match size with
        | Small -> 100u
        | Medium -> 200u
        | Large -> 400u

type PersonSteps(instanceProvider: IInstanceProvider, bowl: DogBowl) =
    [<Given>]
    member this.``I have a (.*) dog`` (size) =
        match size with
        | "large" -> instanceProvider.RegisterInstance typeof<Dog> (Dog(Large))
        | "medium" -> instanceProvider.RegisterInstance typeof<Dog> (Dog(Medium))
        | "small" -> instanceProvider.RegisterInstance typeof<Dog> (Dog(Small))
        | _ -> Assert.Fail(sprintf "Unsupported dog size: %s" size)

    [<When>]
    member this.``I fill the dog bowl with (.*)g of food`` (amount:uint32) =
        bowl.FoodAmount <- amount

type DogSteps(bowl: DogBowl, dog: Dog) =
    [<When>]
    member this.``The dog eats the food from bowl`` () =
        bowl.Eat(dog.AmountToEat)

type BowlSteps(bowl: DogBowl) =
    [<Then>]
    member this.``The bowl contains (.*)g of food`` (amount:int) =
        Assert.AreEqual(amount, bowl.FoodAmount)