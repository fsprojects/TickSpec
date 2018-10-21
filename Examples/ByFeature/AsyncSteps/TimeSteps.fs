module TimeSteps

open NUnit.Framework
open TickSpec
open System.Net
open System
open FSharp.Control.Tasks.V2.ContextInsensitive
open System.Threading.Tasks

type Time =
    | Time of DateTime

let [<Given>] ``having current time`` () =
    Time DateTime.Now

let [<When>] ``I sleep for (\d*)ms using Async`` (duration: int) =
    async {
        do! Async.Sleep duration
    }

let [<When>] ``I sleep for (\d*)ms using Tasks`` (duration: int) =
    task {
        do! Task.Delay duration
    }

let [<Then>] ``the current time is at least (\d*)ms higher than it was`` (duration: int) (Time previousCurrentTime) =  
    int (DateTime.Now - previousCurrentTime).TotalMilliseconds >= duration
    |> Assert.True