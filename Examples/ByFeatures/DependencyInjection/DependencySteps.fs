module DependencySteps

open Dependencies
open TickSpec
open NUnit.Framework

type DependencyFixture () = inherit TickSpec.NUnit.FeatureFixture("Dependency.feature")

type public StepsWithoutImplementation() =
    [<Given>] 
    member this.``I use the first implementation`` () =
        ()

    [<Given>]
    member this.``I use the second implementation`` () =
        ()

type public StepsWithImplementation(dependency: IDependency) =
    [<When>]
    member this.``I store "(.*)"`` (text:string) =
        dependency.Value <- text

    [<Then>]
    member this.``I retrieve "(.*)"`` (text:string) =
        Assert.AreEqual(text, dependency.Value)