module CourseStepDefinitions

open System.Diagnostics
open TickSpec

let mutable topics = Map.empty
let mutable searchTopic = None

let [<Given>] ``there are 240 courses which do not have the topic "(.*)"`` 
        (topic:string) =
    ()
    
let [<Given>] ``there are (.*) courses (.*) that each have "(.*)" as one of the topics`` 
        (courseCount:int, courses:string, topic:string) =
    let xs = courses.Split([|','|]) |> Array.map (fun x -> x.Trim())
    topics <- topics |> Map.add topic xs
        
let [<When>] ``I search for "(.*)"`` (topic:string) =
    searchTopic <- Some topic
    
let [<Then>] ``I should see the following courses:`` (table:Table) =
    match searchTopic with
    | Some topic ->
        let courses = Map.find topic topics
        table.Rows |> Seq.iter (fun cols ->      
            let code = cols.[0]      
            Debug.Assert(Array.exists ((=) code) courses)
        )
    | None -> invalidOp ("No search topic")
