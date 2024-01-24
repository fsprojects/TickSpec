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
    "After scenario" :: previousMessages.Messages
    |> Seq.rev
    |> Seq.iter Console.WriteLine
