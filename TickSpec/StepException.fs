namespace TickSpec

/// Step specific exception
type StepException (message,line:int,scenario:string) =
    inherit System.Exception(message)
    member this.LineNumber = line
    member this.Scenario = string

