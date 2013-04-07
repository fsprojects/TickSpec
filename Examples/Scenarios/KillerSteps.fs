module KillerSteps

type KillerFixture () = inherit TickSpec.NUnit.FeatureFixture("Killer.feature")

open TickSpec
open NUnit.Framework
  
let mutable remaining = 3
let mutable killed = 0 

let [<BeforeScenario;StepScope(Feature="Killer")>] Setup () =
    killed <- killed + 1 

let [<AfterScenario;StepScope(Feature="Killer")>] TearDown () =
    remaining <- remaining - 1

let [<When>] ``this scenario is executed`` () =
    ()

let [<Then>] ``Chuck Norris should expect (\d+) ninjas`` (ninjas:int) =
    Assert.AreEqual(ninjas, remaining)

let [<Then>] ``Chuck Norris should kill one ninja`` () =
    Assert.AreEqual(1, killed)

let [<Then>] ``he should kill (\d+) ninjas`` (ninjas:int) =
    Assert.AreEqual(ninjas, killed)
