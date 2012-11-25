namespace TickSpec

type FeatureSource =
    { 
        Name: string; 
        Scenarios: ScenarioSource [] 
    }
and ScenarioSource =
    { 
        Name: string; 
        Tags: string[];
        Steps: StepSource []; 
        Parameters: (string * string) [] 
    }
    with
    override this.ToString() = this.Name
and StepSource = StepType * LineSource
and StepType =
    | GivenStep of string
    | WhenStep of string
    | ThenStep of string
and LineSource =
    {       
        Number : int
        Text : string
        Bullets : string[] option
        Table : Table option
    }
and Table (header:string[],rows:string[][]) =
    new (header) = Table(header,[|[||]|])
    new () = Table([||]) 
    member this.Header = header
    member this.Rows = rows