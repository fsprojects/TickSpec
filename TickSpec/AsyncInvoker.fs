namespace TickSpec
open System.Threading.Tasks

type AsyncInvoker() =
    static member DoTaskCall (task: Task) =
        async {
            do! task |> Async.AwaitTask
        } |> Async.RunSynchronously
    
    static member DoCallAsync<'T> (input: Task<'T>) =
        async {
            return! input |> Async.AwaitTask
        } |> Async.RunSynchronously
    
    static member DoAsyncCall<'T> (input: Async<'T>) =
        async {
            return! input
        } |> Async.RunSynchronously