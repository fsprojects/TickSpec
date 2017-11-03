namespace Dependency

open TickSpec
open NUnit.Framework

type DogSteps(bowl: DogBowl, dog: Dog) =
    [<When>]
    member this.``The dog eats the food from bowl`` () =
        bowl.Eat(dog.AmountToEat)