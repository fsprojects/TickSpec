namespace TickSpec

open System
open System.Collections.Generic
open System.Diagnostics

/// Provides an instance provider for tests
type IInstanceProvider =
    /// Resolves an instance of desired type
    abstract member Resolve : Type -> obj

    /// Resolves an instance of desired type
    abstract member Resolve<'T> : unit -> 'T

    /// Registers an implementation for an interface
    abstract member RegisterTypeAs<'TType,'TInterface> : unit -> unit

    /// Registers an instance for an interface
    abstract member RegisterInstanceAs<'TInterface> : 'TInterface -> unit

/// Creates instance service provider
type ServiceProvider () as self =
    /// Type instances constructed for invoked steps
    let instances = Dictionary<_,_>()
    /// Resolves an instance
    let rec resolveInstance (t:Type) (typeStack: Type list) =
        match t with
        | t when t = typeof<IInstanceProvider> -> self :> obj
        | t ->
            match instances.TryGetValue t with
            | true, instance -> instance
            | false, _ ->
                let instance = createInstance t typeStack
                instances.Add(t, instance)
                instance

    /// Creates an instance if there was none
    and createInstance (t:Type) (typeStack: Type list) =
        let instance = Activator.CreateInstance t
        instance

    /// Gets type instance for specified type
    [<DebuggerStepThrough>]
    let getInstance (t:Type) =
        resolveInstance t []

    interface IInstanceProvider with
        [<DebuggerStepThrough>]
        member this.Resolve (t:Type) =
            getInstance t

        [<DebuggerStepThrough>]
        member this.Resolve<'T> () =
            getInstance (typeof<'T>) :?> 'T

        [<DebuggerStepThrough>]
        member this.RegisterTypeAs<'TType,'TInterface> () =
            ()

        [<DebuggerStepThrough>]
        member this.RegisterInstanceAs<'TInterface> (instance:'TInterface) =
            ()
    interface System.IServiceProvider with
        [<DebuggerStepThrough>]
        member this.GetService(t:Type) =
            getInstance t
