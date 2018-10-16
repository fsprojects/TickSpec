module HangmanSteps

open Hangman
open TickSpec.Functional
open NUnit.Framework

let performStep (word,guesses) = function
   | Given "the word '(.*)'" [word] ->     
      word,guesses
   | Given "no guesses" [] ->
      word,[]
   | When "the letter '(.*)' is guessed" [Char letter] ->
      word, letter::guesses   
   | Then "the display word is '(.*)'" [expected] ->
      let actual = toPartialWord word guesses
      Assert.AreEqual(expected, actual)
      word, guesses
   | Then "the tally is (.*)" [Int expected] ->
      let actual = tally word guesses
      Assert.AreEqual(expected, actual)
      word, guesses
   | _ -> notImplemented ()
   
open TickSpec.NUnit
open TickSpec

[<TestFixture>]
type HangmanFeature () =
    inherit FeatureFixture<State>()
    [<Test>]
    [<TestCaseSource("Scenarios")>]
    member __.TestScenario (scenario:ScenarioSource) =
        FeatureFixture<State>.PerformTest scenario performStep (fun () -> "",[])
    static member Scenarios =
        FeatureFixture<State>.MakeScenarios "Functional.Hangman.feature"
and State = string * char list