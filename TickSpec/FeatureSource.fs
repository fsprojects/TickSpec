namespace TickSpec

open TickSpec.LineParser

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
and StepSource = StepType * Line