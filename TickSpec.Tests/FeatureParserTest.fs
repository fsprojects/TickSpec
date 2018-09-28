module TickSpec.Test.FeatureParserTest

open NUnit.Framework
open TickSpec
open System

let verifyParsing (fileContent: string) (expected: FeatureSource) =
    let featureSource = fileContent.Split([|"\n"|], StringSplitOptions.None) |> FeatureParser.parseFeature
    Assert.AreEqual(expected, featureSource)

[<Test>]
let TagsAndExamples () =
    "
    @http
    Feature: HTTP server

    Background:
        Given User connects to <server>

        @basics @index
        Scenario Outline: Tags and Examples Sc.
            When Client requests <page>
            Then Server responds with page <page>

        @smoke @all
        Examples:
            | server  |
            | smoke   |

        Examples:
            | page         |
            | index.html   |
            | default.html |

    @all
    Shared Examples:
        | server     |
        | testing    |
        | production |
    "
    |> verifyParsing <|
    {
        Name = "HTTP server"
        Scenarios = [|
            {
                Name = "Tags and Examples Sc. (0)"
                Tags = [|"http";"basics";"index";"smoke";"all"|]
                Steps = [|
                    (GivenStep "User connects to smoke", {
                        Number = 6
                        Text = "        Given User connects to <server>"
                        Bullets = None
                        Table = None
                        Doc = None
                    })
                    (WhenStep "Client requests index.html", {
                        Number = 10
                        Text = "            When Client requests <page>"
                        Bullets = None
                        Table = None
                        Doc = None
                    })
                    (ThenStep "Server responds with page index.html", {
                        Number = 11
                        Text = "            Then Server responds with page <page>"
                        Bullets = None
                        Table = None
                        Doc = None
                    })
                |]
                Parameters = [|("server","smoke");("page","index.html")|]
            }
            {
                Name = "Tags and Examples Sc. (1)"
                Tags = [|"http";"basics";"index";"smoke";"all"|]
                Steps = [|
                    (GivenStep "User connects to smoke", {
                        Number = 6
                        Text = "        Given User connects to <server>"
                        Bullets = None
                        Table = None
                        Doc = None
                    })
                    (WhenStep "Client requests default.html", {
                        Number = 10
                        Text = "            When Client requests <page>"
                        Bullets = None
                        Table = None
                        Doc = None
                    })
                    (ThenStep "Server responds with page default.html", {
                        Number = 11
                        Text = "            Then Server responds with page <page>"
                        Bullets = None
                        Table = None
                        Doc = None
                    })
                |]
                Parameters = [|("server","smoke");("page","default.html")|]
            }
            {
                Name = "Tags and Examples Sc. (2)"
                Tags = [|"http";"basics";"index";"all"|]
                Steps = [|
                    (GivenStep "User connects to testing", {
                        Number = 6
                        Text = "        Given User connects to <server>"
                        Bullets = None
                        Table = None
                        Doc = None
                    })
                    (WhenStep "Client requests index.html", {
                        Number = 10
                        Text = "            When Client requests <page>"
                        Bullets = None
                        Table = None
                        Doc = None
                    })
                    (ThenStep "Server responds with page index.html", {
                        Number = 11
                        Text = "            Then Server responds with page <page>"
                        Bullets = None
                        Table = None
                        Doc = None
                    })
                |]
                Parameters = [|("server","testing");("page","index.html")|]
            }
            {
                Name = "Tags and Examples Sc. (3)"
                Tags = [|"http";"basics";"index";"all"|]
                Steps = [|
                    (GivenStep "User connects to testing", {
                        Number = 6
                        Text = "        Given User connects to <server>"
                        Bullets = None
                        Table = None
                        Doc = None
                    })
                    (WhenStep "Client requests default.html", {
                        Number = 10
                        Text = "            When Client requests <page>"
                        Bullets = None
                        Table = None
                        Doc = None
                    })
                    (ThenStep "Server responds with page default.html", {
                        Number = 11
                        Text = "            Then Server responds with page <page>"
                        Bullets = None
                        Table = None
                        Doc = None
                    })
                |]
                Parameters = [|("server","testing");("page","default.html")|]
            }
            {
                Name = "Tags and Examples Sc. (4)"
                Tags = [|"http";"basics";"index";"all"|]
                Steps = [|
                    (GivenStep "User connects to production", {
                        Number = 6
                        Text = "        Given User connects to <server>"
                        Bullets = None
                        Table = None
                        Doc = None
                    })
                    (WhenStep "Client requests index.html", {
                        Number = 10
                        Text = "            When Client requests <page>"
                        Bullets = None
                        Table = None
                        Doc = None
                    })
                    (ThenStep "Server responds with page index.html", {
                        Number = 11
                        Text = "            Then Server responds with page <page>"
                        Bullets = None
                        Table = None
                        Doc = None
                    })
                |]
                Parameters = [|("server","production");("page","index.html")|]
            }
            {
                Name = "Tags and Examples Sc. (5)"
                Tags = [|"http";"basics";"index";"all"|]
                Steps = [|
                    (GivenStep "User connects to production", {
                        Number = 6
                        Text = "        Given User connects to <server>"
                        Bullets = None
                        Table = None
                        Doc = None
                    })
                    (WhenStep "Client requests default.html", {
                        Number = 10
                        Text = "            When Client requests <page>"
                        Bullets = None
                        Table = None
                        Doc = None
                    })
                    (ThenStep "Server responds with page default.html", {
                        Number = 11
                        Text = "            Then Server responds with page <page>"
                        Bullets = None
                        Table = None
                        Doc = None
                    })
                |]
                Parameters = [|("server","production");("page","default.html")|]
            }
        |]
    }