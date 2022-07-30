module ScenarioInformationSteps

open System
open NUnit.Framework
open TickSpec

let [<Then>] ``the test is named "(.+)"`` (testName: string) (scenarioInformation: ScenarioInformation) =
    Assert.AreEqual(testName, scenarioInformation.Name)

let [<Then>] ``the test is tagged "(.+)"`` (expectedTag: string) (scenarioInformation: ScenarioInformation) =
    CollectionAssert.Contains(scenarioInformation.Tags, expectedTag)
