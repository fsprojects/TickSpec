module WebSteps

open NUnit.Framework
open TickSpec
open System.Net
open System
open FSharp.Control.Tasks.V2.ContextInsensitive

type DownloadedPage =
    | DownloadedPage of string

let [<When>] ``I download (.*) web page using Async`` address =
    async {
        let req = WebRequest.Create(Uri address) 
        use! resp = req.AsyncGetResponse()
        use stream = resp.GetResponseStream() 
        use reader = new IO.StreamReader(stream)
        return reader.ReadToEnd() |> DownloadedPage
    }

let [<When>] ``I download (.*) web page using Tasks`` address =
    task {
        let req = WebRequest.Create(Uri address) 
        use! resp = req.GetResponseAsync()
        use stream = resp.GetResponseStream() 
        use reader = new IO.StreamReader(stream)
        return reader.ReadToEnd() |> DownloadedPage
    }

let [<Then>] ``the downloaded page contains "(.*)"`` (text: string) (DownloadedPage page) =  
    page.Contains(text)
    |> Assert.True

