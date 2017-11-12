module ByFeature.DependencyInjection.Feature

open NUnit.Framework
open TickSpec

let definitions = AssemblyStepDefinitionsSource(System.Reflection.Assembly.GetExecutingAssembly())

let dogFeeding = definitions.ScenariosFromEmbeddedResource "DogFeeding.feature"

[<Test; TestCaseSource("dogFeeding")>]
let public DogFeeding(scenario : Scenario) = Runner.run scenario