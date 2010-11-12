module internal TickSpec.LineParser

open System.Text.RegularExpressions

type ScenarioType =
    | Named of string
    | Background
    | Shared
    with 
    override this.ToString() =
        match this with
        | Named s -> s
        | Background -> "Background"
        | Shared -> "Shared Examples"

/// Item type
type internal ItemType =
    | BulletPoint of string
    | TableRow of string[]

/// Line type
type internal LineType = 
    | ScenarioStart of ScenarioType
    | ExamplesStart
    | GivenStep of string
    | WhenStep of string
    | ThenStep of string   
    | Item of LineType * ItemType       
    | Tag of string 

/// Try single parameter regular expression
let tryRegex input pattern =
    let m = Regex.Match(input, pattern)
    if m.Success then m.Groups.[1].Value |> Some
    else None

let (|Scenario|_|) (s:string) = 
    let s = s.Trim()
    if s.StartsWith("Scenario") || s.StartsWith("Story") then
        Scenario s |> Some else None
let (|IsBackground|_|) s = 
    tryRegex s "Background(.*)" 
    |> Option.map (fun t -> IsBackground) 
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
let (|Bullet|_|) (s:string) =    
    if s.Trim().StartsWith("*") then 
        s.Substring(s.IndexOf("*")+1).Trim() |> Some
    else None
let (|SharedExamples|_|) (s:string) =
    if s.Trim().StartsWith("Shared Examples") then Some SharedExamples else None      
let (|Examples|_|) (s:string) =
    if s.Trim().StartsWith("Examples") then Some Examples else None
let (|Attribute|_|) (s:string) =
    if s.Trim().StartsWith("@") then 
        Attribute (s.Substring(s.IndexOf("@")+1).Trim()) |> Some
    else None
    
/// Line state given previous line state and new line text
let parseLine = function             
    | _, Scenario text -> ScenarioStart (Named(text)) |> Some   
    | _, IsBackground -> ScenarioStart Background |> Some
    | _, SharedExamples -> ScenarioStart Shared |> Some   
    | _, Examples -> ExamplesStart |> Some
    | ScenarioStart (Named _), Given text    
    | ScenarioStart Background, Given text
    | GivenStep _, Given text | Item(GivenStep _,_), Given text
    | GivenStep _, And text | Item(GivenStep _,_), And text 
    | GivenStep _, But text | Item(GivenStep _,_), But text 
        -> GivenStep text |> Some
    | ScenarioStart (Named _), When text
    | ScenarioStart Background, When text 
    | GivenStep _, When text | Item(GivenStep _,_), When text
    | WhenStep _, When text | Item(WhenStep _,_), When text
    | WhenStep _, And text | Item(WhenStep _,_), And text
    | WhenStep _, But text | Item(WhenStep _,_), But text
        -> WhenStep text |> Some
    | ScenarioStart (Named _), Then text  
    | ScenarioStart Background, Then text
    | GivenStep _, Then text | Item (GivenStep _,_), Then text
    | WhenStep _, Then text | Item (WhenStep _,_), Then text
    | ThenStep _, Then text | Item (ThenStep _,_), Then text
    | ThenStep _, And text | Item(ThenStep _,_), And text
    | ThenStep _, But text | Item(ThenStep _,_), But text
        -> ThenStep text |> Some
    | (GivenStep _ as line), Bullet xs 
    | (WhenStep _ as line), Bullet xs 
    | (ThenStep _ as line), Bullet xs
    | Item (line, BulletPoint(_)), Bullet xs ->
        Item(line, BulletPoint xs) |> Some
    | (ScenarioStart Shared as line), Row xs
    | (ExamplesStart as line), Row xs
    | (GivenStep _ as line), Row xs 
    | (WhenStep _ as line), Row xs 
    | (ThenStep _ as line), Row xs ->
        Item(line, TableRow xs) |> Some
    | Item (line, TableRow ys), Row xs when ys.Length = xs.Length ->     
        Item(line, TableRow xs) |> Some   
    | _, Attribute text ->
        Tag text |> Some        
    | _, line -> None

let expectingLine = function
    | ScenarioStart _ -> "Expecting Given, When or Then step"
    | GivenStep _ | Item(GivenStep _,_) -> 
        "Expecting Table row, Bullet, Given, When, Then, And or But step"
    | WhenStep _ | Item(WhenStep _,_) -> 
        "Expecting Table row, Bullet, When, Then, And or But step"
    | ThenStep _ | Item(ThenStep _,_) -> 
        "Expecting Table row, Bullet, Then, And or But step"
    | ExamplesStart -> "Expecting Table row"
    | Item(_,_) -> "Unexpected or invalid line"                       
    | Tag _ -> "Unexpected line"
