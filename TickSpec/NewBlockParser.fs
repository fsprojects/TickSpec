module internal TickSpec.NewBlockParser

type internal FeatureBlock =
    {
        Name: string
        Tags: string list
        Description: string option
        Background: StepBlock list
        Scenarios: ScenarioBlock list
        SharedExamples: ExampleBlock list
    }
and internal ScenarioBlock =
    {
        Name: string
        Tags: string list
        Steps: StepBlock list
        Examples: ExampleBlock list
    }
and internal StepBlock =
    {
        Step: StepType
        LineNumber: int
        LineString: string
        Item: ItemBlock option
    }
and internal ItemBlock =
    | BulletsItem of string list
    | TableItem of TableBlock
    | DocStringItem of string
and internal TableBlock =
    {
        Header: string list
        Rows: string list list
    }
and internal ExampleBlock =
    {
        Tags: string list
        Table: TableBlock
    }

let parseBlocks (lines:string seq) =
    {
        Name = "N/A"
        Tags = []
        Description = None
        Background = []
        Scenarios = []
        SharedExamples = []
    }

