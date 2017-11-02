module Dependency.DogSteps

open TickSpec
    
let [<When>] ``The dog eats the food from bowl`` (bowl: DogBowl) (dog:Dog) =
    bowl.Eat(dog.AmountToEat)