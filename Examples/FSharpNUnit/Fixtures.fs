module Fixtures

open NUnit.TickSpec

type Feature2 () = inherit FeatureFixture("Feature2.txt")

type TicTacToe () = inherit FeatureFixture("TicTacToe.txt")