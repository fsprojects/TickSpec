namespace TickSpec

open System
open System.Collections.Generic

/// Creates instance service provider
type ServiceProvider () =
    /// Creates new instance
    let CreateInstance (t:Type) =
        let cons = t.GetConstructor([||])
        cons.Invoke([||])
    /// Type instances constructed for invoked steps
    let instances = Dictionary<_,_>()
    /// Gets type instance for specified type
    let getInstance (t:Type) =
        match instances.TryGetValue t with
        | true, instance -> instance
        | false, _ ->
            let instance = CreateInstance t
            instances.Add(t,instance)
            instance
    interface System.IServiceProvider with
        member this.GetService(t:Type) =
            getInstance t
