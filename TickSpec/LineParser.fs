module internal TickSpec.LineParser

open System.Text.RegularExpressions

/// Line type
type internal LineType = 
    | ScenarioStart of string
    | ExamplesStart
    | GivenStep of string
    | WhenStep of string
    | ThenStep of string   
    | TableRow of string[]        

/// Try single parameter regular expression
let tryRegex input pattern =
    let m = Regex.Match(input, pattern)
    if m.Success then m.Groups.[1].Value |> Some
    else None

let (|Scenario|_|) s = 
    tryRegex s "Scenario(.*)" 
    |> Option.map (fun t -> Scenario t)   
let (|Given|_|) s = 
    tryRegex s "Given\s+(.*)" 
    |> Option.map (fun t -> Given t)
let (|When|_|) s = 
    tryRegex s "When\s+(.*)" 
    |> Option.map (fun t -> When t)
let (|Then|_|) s = 
    tryRegex s "Then\s+(.*)" 
    |> Option.map (fun t -> Then t)
let (|And|_|) s = 
    tryRegex s "And\s+(.*)" 
    |> Option.map (fun t -> And t)
let (|But|_|) s = 
    tryRegex s "But\s+(.*)" 
    |> Option.map (fun t -> But t) 
let (|Row|_|) (s:string) =    
    if s.Trim().StartsWith("|") then 
        let options = System.StringSplitOptions.RemoveEmptyEntries
        let cols = s.Trim().Split([|'|'|],options)
        let cols = cols |> Array.map (fun s -> s.Trim())
        Row cols |> Some
    else None
let (|Examples|_|) (s:string) =
    if s.Trim().StartsWith("Examples") then Some Examples else None

/// Line state given previous line state and new line text
let parseLine = function         
    | _, Scenario text ->
        ScenarioStart text    
    | _, Examples text ->
        ExamplesStart 
    | ScenarioStart _, Given text     
    | GivenStep _, Given text 
    | GivenStep _, And text | GivenStep _, But text -> 
        GivenStep text
    | ScenarioStart _, When text | TableRow _, When text
    | GivenStep _, When text | WhenStep _, When text 
    | WhenStep _, And text | WhenStep _, But text ->               
        WhenStep text
    | ScenarioStart _, Then text | TableRow _, Then text 
    | GivenStep _, Then text
    | WhenStep _, Then text | ThenStep _, Then text
    | ThenStep _, And text | ThenStep _, But text -> 
        ThenStep text        
    | ExamplesStart _, Row xs
    | GivenStep _, Row xs
    | WhenStep _, Row xs 
    | ThenStep _, Row xs
    | TableRow _, Row xs ->
        TableRow xs
    | _, line -> invalidOp line
