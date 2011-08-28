namespace TickSpec

open System
open System.Collections.Generic
open System.Diagnostics

/// Creates instance service provider
type ServiceProvider () =
    /// Type instances constructed for invoked steps
    let instances = Dictionary<_,_>()
    /// Gets type instance for specified type
    [<DebuggerStepThrough>]
    let getInstance (t:Type) =
        match instances.TryGetValue t with
        | true, instance -> instance
        | false, _ ->
            let instance = Activator.CreateInstance t
            instances.Add(t,instance)
            instance
    interface System.IServiceProvider with
        [<DebuggerStepThrough>]
        member this.GetService(t:Type) =
            getInstance t
