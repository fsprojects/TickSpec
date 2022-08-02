module ScenarioMetadataSteps

open System
open NUnit.Framework
open TickSpec

let [<Then>] ``the test is named "(.+)"`` (testName: string) (metadata: ScenarioMetadata) =
    Assert.AreEqual(testName, metadata.Name)

let [<Then>] ``the test is tagged "(.+)"`` (expectedTag: string) (metadata: ScenarioMetadata) =
    CollectionAssert.Contains(metadata.Tags, expectedTag)
