module TickSpec.Build.GherkinParser

open System
open System.IO

[<AutoOpen>]
module private Impl = 
    let trimLine numSpaces (line:string) =
        if line |> String.IsNullOrWhiteSpace then
            String.Empty
        else
            line.Substring(numSpaces).TrimEnd()

    // indent which exists for all non-empty lines
    let detectGlobalIndent lines =
        lines
        |> Seq.filter (String.IsNullOrWhiteSpace >> not)
        |> Seq.map(fun x -> x |> Seq.takeWhile Char.IsWhiteSpace |> Seq.length)
        |> Seq.min

    // Line numbers start at 1 as in any editor
    let parseLines (text:string)=
        let lines = text.Split(Environment.NewLine)

        let globalIndent = lines |> detectGlobalIndent

        lines
        |> Seq.mapi(fun i l -> i + 1, l |> trimLine globalIndent)
        |> List.ofSeq

    let (|Title|_|) (keyword:string) (line:string) =
        let prefix = keyword + ":"
        if line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) then
            line.Substring(prefix.Length).Trim() |> Some
         else
            None

    let (|Tags|_|) (line:string) =
        if line.Trim().StartsWith("@") then
            line.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            |> Seq.map(fun x -> x.Trim().TrimStart('@'))
            |> List.ofSeq
            |> Some
        else
            None

    let (|Comment|_|) (line:string) =
        if line.Trim().StartsWith("#") then
            line.TrimStart().TrimStart('#').Trim() |> Some
        else
            None

    let trimEmptyLines =
        Seq.skipWhile String.IsNullOrWhiteSpace
        >> Seq.rev
        >> Seq.skipWhile String.IsNullOrWhiteSpace
        >> Seq.rev

    let parseTags (lines:(int*string) list) lineNo =
        if lineNo > 0 then
            match lines |> Seq.find(fun (x,_) -> x = lineNo - 1) |> snd with
            | Tags x -> x
            | _ -> []
        else
            []

    let parseComment (lines:(int*string) list) lineNo =
        if lineNo > 0 then
            lines
            |> Seq.takeWhile(fun (x,_) -> x < lineNo)
            |> Seq.rev
            |> Seq.takeWhile(fun (_,x) -> String.IsNullOrWhiteSpace(x) |> not)
            |> Seq.map(fun (_,x) -> x.Trim())
            |> Seq.choose(function | Comment x -> x |> Some | _ -> None)
            |> Seq.rev
            |> String.concat " "
        else
            ""

    let getProjectLocalFolders (file:string) =
        let rec find dir = 
            seq {
                if Directory.GetFiles(dir, "*.fsproj").Length > 0 then
                    yield! []
                else
                    yield! dir |> Path.GetFileName |> List.singleton
                    yield! dir |> Path.GetDirectoryName |> find 
            }
                
        
        file |> Path.GetDirectoryName |> find |> List.ofSeq

let Parse location (feature:string) =
    let linesWithLineNo = feature |> parseLines

    let featureName =
        linesWithLineNo
        |> Seq.map snd
        |> Seq.choose(function | Title "Feature" x -> x |> Some | _ -> None)
        |> Seq.exactlyOne

    let scenarios = 
        linesWithLineNo
        |> Seq.filter(snd >> function | Tags _ -> false | Comment _ -> false | _ -> true)
        |> Seq.mapFold(fun scenario (lineNo, line) ->
            match scenario, line with
            | _, Title "Scenario" x
            | _, Title "Scenario Outline" x ->
                let newScenario =
                    { 
                        Name = line
                        Title = x
                        StartsAtLine = lineNo + 1 // skip scenario title
                        Body = []
                        Tags = parseTags linesWithLineNo lineNo
                        Description = parseComment linesWithLineNo lineNo
                    }
                scenario, newScenario |> Some
            | Some scenario, _ -> None, { scenario with Body = line::scenario.Body } |> Some
            | None, _ -> None, None // ignore lines outside scenario
            ) None
        |> fun (scenarios, scenario) -> scenario |> List.singleton |> Seq.append scenarios
        |> Seq.choose id
        |> Seq.map(fun x -> 
            let globalIndent = x.Body |> detectGlobalIndent
            { x with Body = x.Body |> Seq.map (trimLine globalIndent) |> trimEmptyLines |> Seq.rev |> List.ofSeq } )
        |> List.ofSeq

    let background = 
        linesWithLineNo
        |> Seq.map snd
        |> Seq.filter(function | Tags _ -> false | Comment _ -> false | _ -> true)
        |> Seq.skipWhile ((function | Title "Background" _ -> true | _ -> false) >> not)
        |> Seq.takeWhile ((function | Title "Scenario" _ -> true | Title "Scenario Outline" _ -> true | _ -> false) >> not)
        |> trimEmptyLines
        |> List.ofSeq 
        |> function
            | [] -> []
            | h::t -> 
                let globalIndent = t |> detectGlobalIndent
                t |> List.map (trimLine globalIndent)
    
    {
        Name = featureName
        Background = background
        Location = location
        Scenarios = scenarios
    }

let Read (file:string) =
    let location = 
        {
            Filename = file |> Path.GetFileName
            Folders = file |> getProjectLocalFolders
        }

    file |> File.ReadAllText |> Parse location

let FindAllFeatureFiles folder =
    let subDirectoriesToSkip = 
        [
            "node_modules"
            "obj"
            "bin"
            "dist"
        ]

    let rec getAllFiles dir =
        seq { 
            yield! Directory.EnumerateFiles(dir, "*.feature")
            for subDir in Directory.EnumerateDirectories(dir) do
                let dirName = Path.GetFileName(subDir)
                if subDirectoriesToSkip |> Seq.exists(fun x -> x.Equals(dirName, StringComparison.OrdinalIgnoreCase)) |> not then
                    yield! getAllFiles subDir
        }

    folder 
    |> getAllFiles
    |> List.ofSeq
