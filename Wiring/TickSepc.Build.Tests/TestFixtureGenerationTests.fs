module TickSepc.Build.Tests.TestFixtureGenerationTests

open NUnit.Framework
open TickSpec.Build
open FsUnit

[<Test>]
let ``Single scenario``() =
    [
        """
        Feature: First feature

        Scenario: One
        GIVEN some environment
        WHEN some event happens
        THEN the system should be in this state
        """
    ]
    |> TestApi.GenerateTestFixtures "Dummy.feature"
    |> should haveSubstringIgnoringWhitespaces  """
        namespace Specification

        open System.Reflection
        open NUnit.Framework
        open TickSpec.CodeGen

        [<TestFixture>]
        type ``First feature``() = 
            inherit AbstractFeature()

            let scenarios = AbstractFeature.GetScenarios(Assembly.GetExecutingAssembly(), "Dummy.feature")

            [<Test>]
            member this.``One``() =
        #line 5 "Dummy.feature"
                this.RunScenario(scenarios, "Scenario: One")
            """

[<Test>]
let ``Feature file in sub folder``() =
    [
        """
        Feature: First feature

        Scenario: One
        GIVEN some environment
        WHEN some event happens
        THEN the system should be in this state
        """
    ]
    |> TestApi.GenerateTestFixtures "SubFeature/Dummy.feature"
    |> should haveSubstringIgnoringWhitespaces  """
        namespace Specification

        open System.Reflection
        open NUnit.Framework
        open TickSpec.CodeGen

        [<TestFixture>]
        type ``First feature``() = 
            inherit AbstractFeature()

            let scenarios = AbstractFeature.GetScenarios(Assembly.GetExecutingAssembly(), "SubFeature.Dummy.feature")

            [<Test>]
            member this.``One``() =
        #line 5 "SubFeature/Dummy.feature"
                this.RunScenario(scenarios, "Scenario: One")
            """



[<Test>]
let ``With background``() =
    [
        """
        Feature: First feature

        Background:
            GIVEN some additional environment

        Scenario: One
        GIVEN some environment
        WHEN some event happens
        THEN the system should be in this state
        """
    ]
    |> TestApi.GenerateTestFixtures "Dummy.feature"
    |> should haveSubstringIgnoringWhitespaces  """
        namespace Specification

        open System.Reflection
        open NUnit.Framework
        open TickSpec.CodeGen

        [<TestFixture>]
        type ``First feature``() = 
            inherit AbstractFeature()

            let scenarios = AbstractFeature.GetScenarios(Assembly.GetExecutingAssembly(), "Dummy.feature")

            [<Test>]
            member this.``One``() =
        #line 8 "Dummy.feature"
                this.RunScenario(scenarios, "Scenario: One")
            """

[<Test>]
let ``Multiple features with multipe scenario``() =
    [
        """
        Feature: First feature

        Scenario: One
        GIVEN some environment
        WHEN some event happens
        THEN the system should be in this state

        Scenario: Two
        GIVEN some environment
        WHEN some event happens
        THEN the system should be in this state
        """

        """
        Feature: Second feature

        Scenario: Three
        GIVEN some environment
        WHEN some event happens
        THEN the system should be in this state

        Scenario: Four
        GIVEN some environment
        WHEN some event happens
        THEN the system should be in this state
        """
    ]
    |> TestApi.GenerateTestFixtures "Dummy.feature"
    |> should haveSubstringIgnoringWhitespaces  """
        namespace Specification

        open System.Reflection
        open NUnit.Framework
        open TickSpec.CodeGen

        [<TestFixture>]
        type ``First feature``() = 
            inherit AbstractFeature()

            let scenarios = AbstractFeature.GetScenarios(Assembly.GetExecutingAssembly(), "Dummy.feature")

            [<Test>]
            member this.``One``() =
        #line 5 "Dummy.feature"
                this.RunScenario(scenarios, "Scenario: One")

            [<Test>]
            member this.``Two``() =
        #line 10 "Dummy.feature"
                this.RunScenario(scenarios, "Scenario: Two")

        [<TestFixture>]
        type ``Second feature``() = 
            inherit AbstractFeature()

            let scenarios = AbstractFeature.GetScenarios(Assembly.GetExecutingAssembly(), "Dummy.feature")

            [<Test>]
            member this.``Three``() =
        #line 5 "Dummy.feature"
                this.RunScenario(scenarios, "Scenario: Three")

            [<Test>]
            member this.``Four``() =
        #line 10 "Dummy.feature"
                this.RunScenario(scenarios, "Scenario: Four")
            """

[<Test>]
let ``Scenario outline``() =
    [
        """
        Feature: First feature

        Scenario Outline: Computing the state
            GIVEN a work item
            AND with "Concept Needed" set to '<ConceptNeeded>'
            WHEN parsing the work item
            THEN the computed concept state is '<ConceptState>'

            Examples:
            | ConceptNeeded | ConceptState |
            |               | Unset        |
            | yes           | Needed       |
            | no            | NotNeeded    |
        """
    ]
    |> TestApi.GenerateTestFixtures "Dummy.feature"
    |> should haveSubstringIgnoringWhitespaces  """
        namespace Specification

        open System.Reflection
        open NUnit.Framework
        open TickSpec.CodeGen

        [<TestFixture>]
        type ``First feature``() = 
            inherit AbstractFeature()

            let scenarios = AbstractFeature.GetScenarios(Assembly.GetExecutingAssembly(), "Dummy.feature")

            [<Test>]
            member this.``Computing the state``() =
        #line 5 "Dummy.feature"
                this.RunScenario(scenarios, "Scenario Outline: Computing the state")
            """
