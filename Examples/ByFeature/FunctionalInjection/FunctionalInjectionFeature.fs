module ByFeature.FunctionalInjection.Feature

open NUnit.Framework
open TickSpec

let definitions = AssemblyStepDefinitionsSource(System.Reflection.Assembly.GetExecutingAssembly())

let shopping = definitions.ScenariosFromEmbeddedResource "Shopping.feature"
let dogFeeding = definitions.ScenariosFromEmbeddedResource "DogFeeding.feature"
let stock = definitions.ScenariosFromEmbeddedResource "Stock.feature"

[<Test; TestCaseSource("shopping")>]
let public Shopping(scenario : Scenario) = Runner.run scenario

[<Test; TestCaseSource("dogFeeding")>]
let public FunctionalDogFeeding(scenario : Scenario) = Runner.run scenario

[<Test; TestCaseSource("stock")>]
let public Stock(scenario : Scenario) = Runner.run scenario