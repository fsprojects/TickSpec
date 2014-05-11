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
type HangmanFeature () =
    inherit FeatureFixture<State>("Hangman.feature", performStep, fun () -> "",[])
and State = string * char list