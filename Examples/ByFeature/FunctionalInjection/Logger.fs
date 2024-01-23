module Logger

open TickSpec
open System

type LogMessages = string list

type LoggerContext = { Messages: LogMessages }

let [<BeforeScenario>] setup () = 
    { Messages = "Before scenario" |> List.singleton }

let [<BeforeStep>] beforeStep (previousMessages: LoggerContext) =
    { Messages = "Before step" :: previousMessages.Messages  }

let [<AfterStep>] afterStep (previousMessages: LoggerContext) =
    { Messages = "After step" :: previousMessages.Messages  }

let [<AfterScenario>] afterScenario (previousMessages: LoggerContext) =
    let allMessages =
        "After scenario" :: previousMessages.Messages
        |> List.rev
    allMessages |> List.iter Console.WriteLine
