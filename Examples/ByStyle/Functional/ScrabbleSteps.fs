module ScrabbleSteps

open Scrabble
open TickSpec.Functional
open NUnit.Framework

let performStep properties step =
    match step with
    | Given "an empty scrabble board" [] -> []
    | When "player (\d+) plays \"([A-Z]+)\" at (\d+[A-Z])"
        [Int player;word;location] ->
        Word(word)::properties
    | When "player (\d+) prefixes \"([A-Z]+)\" with \"([A-Z]+)\" at (\d+[A-Z])"
        [Int player;prefix;word;location] ->
        Word(prefix+word)::properties
    | When "forms ([A-Z]+)" [word] ->
        Word(word)::properties
    | When "([A-Z]) is on a DLS" [Char letter] ->    
        DLS(letter)::properties
    | When "([A-Z]) is on a TWS" [Char letter] ->
        TWS::properties 
    | When "([A-Z]) is on the center star" [Char letter] ->
        CenterStar::properties
    | Then "he scores (\d+)" [Int score] ->
        Assert.AreEqual(score, total properties)
        properties
    | _ -> notImplemented()

open TickSpec.NUnit
open TickSpec

[<TestFixture>]
type ScrabbleFixture () =
    inherit FeatureFixture<Property list>()
    [<Test>]
    [<TestCaseSource("Scenarios")>]
    member __.TestScenario (scenario:ScenarioSource) =
        FeatureFixture<Property list>.PerformTest scenario performStep (fun () -> [])
    static member Scenarios =
        FeatureFixture<Property list>.MakeScenarios "Functional.Scrabble.feature"
