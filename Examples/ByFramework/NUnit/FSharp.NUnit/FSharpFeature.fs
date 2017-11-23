module ByFramework.NUnit.FSharp.Feature

open TickSpec.NUnit

type Feature2() = inherit FeatureFixture("Stock.feature")

type TicTacToe() = inherit FeatureFixture("TicTacToe.feature")