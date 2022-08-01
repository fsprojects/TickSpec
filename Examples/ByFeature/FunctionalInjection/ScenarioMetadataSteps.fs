module ScenarioMetadataSteps

open System
open NUnit.Framework
open TickSpec

let [<Then>] ``the test is named "(.+)"`` (testName: string) (scenarioMetadata: ScenarioMetadata) =
    Assert.AreEqual(testName, scenarioMetadata.Name)

let [<Then>] ``the test is tagged "(.+)"`` (expectedTag: string) (scenarioMetadata: ScenarioMetadata) =
    CollectionAssert.Contains(scenarioMetadata.Tags, expectedTag)
