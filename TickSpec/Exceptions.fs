namespace TickSpec

/// Step specific exception
type StepException (message,line:int,scenario:string) =
    inherit System.Exception(message)
    member this.LineNumber = line
    member this.Scenario = scenario

/// Exception occured during parsing of the feature file
type ParseException (message,line:int option) =
    inherit System.Exception(message)
    member this.LineNumber = line
