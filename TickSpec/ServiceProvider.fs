namespace TickSpec

open System
open System.Diagnostics
open System.Reflection

/// Instances storage
[<Sealed>]
type InstanceStore () =
    let mutable instances : list<Type*obj> = []

    /// Prepends key/value to list if not present; existing key with different value is removed
    let distinctAdd key value inList =
        let rec distinctAddRec key value revHead inTail =
            match inTail with
            | [] -> (key, value) :: inList
            | (t, instance:obj) :: rest when key = t ->
                if value <> instance then
                    match instance with
                    | :? IDisposable as d -> d.Dispose()
                    | _ -> ()
                    (key, value) :: (List.rev revHead) @ rest
                else
                    inList
            | head :: rest ->
                distinctAddRec key value (head :: revHead) rest
        distinctAddRec key value [] inList

    let disposeAsync (d:IAsyncDisposable) =
        d.DisposeAsync()
        |> ignore
        ()

    /// Stores element (skips storing existing, old element removed and new prepended)
    member this.Store key value =
        instances <- instances |> distinctAdd key value

    /// Returns value for specified key
    member this.TryGetValue key =
        match instances |> List.tryFind(fun (x,_) -> x = key) with
        | Some elem -> Some(snd elem)
        | None -> None


    interface IDisposable with
        member __.Dispose() =
            instances
            |> Seq.map snd
            |> Seq.iter (function
                | :? IAsyncDisposable as d -> disposeAsync(d)
                | :? IDisposable as d -> d.Dispose()
                | _ -> ())
            instances <- []

/// Provides an instance provider for tests
type IInstanceProvider =
    inherit IServiceProvider

    /// Registers an instance for a type (if there is already a registered instance, it will be replaced)
    abstract member RegisterInstance : Type * obj -> unit

/// <summary>
/// Decorates the supplied <see cref="IServiceProvider" /> to fulfil TickSpec's InstanceProvider contract.
/// </summary>
/// <param name="innerProvider">The provider that will GetInstances of Step Definition classes.</param>
type ExternalServiceProviderInstanceProvider(innerProvider: IServiceProvider) as self =
    /// Type instances for invoked steps
    let instances = new InstanceStore()
    let getInstance (t:Type) =
        match instances.TryGetValue t with
        | Some instance -> instance
        | None -> innerProvider.GetService(t)

    interface IServiceProvider with
        [<DebuggerStepThrough>]
        member __.GetService (t: Type) =
            if t = typeof<IInstanceProvider> then self :> obj
            else getInstance t

    interface IInstanceProvider with
        member __.RegisterInstance (t: Type, instance: obj) =
            instances.Store t instance

    interface IDisposable with
        member __.Dispose() =
            (instances :> IDisposable).Dispose()
            match innerProvider with
            | :? IDisposable as d -> d.Dispose()
            | _ -> ()

[<Sealed>]
/// Creates instance service provider
type InstanceProvider() as self =
    /// Type instances for invoked steps
    let instances = new InstanceStore()

    /// Resolves an instance for a specified type (and remembering the stack of types being resolved)
    let rec resolveInstance (t:Type) (typeStack: Type list) =
        let alreadyRequested =
            typeStack
            |> List.tryFind (fun x -> x = t)

        match alreadyRequested with
        | Some _ -> raise (InvalidOperationException(sprintf "Circular dependency found when resolving type %O" t))
        | None -> ()

        match t with
        | t when t = typeof<IInstanceProvider> -> self :> obj
        | t ->
            match instances.TryGetValue t with
            | Some instance -> instance
            | None ->
                let instance = createInstance t typeStack
                instances.Store t instance
                instance

    /// Creates an instance if there was none for a specified type (and remembering the stack of types being resolved)
    and createInstance (t:Type) (typeStack: Type list) =
        let constructors =
            t.GetConstructors()
            |> List.ofArray

        let (_, widestConstructors) =
            constructors
            |> List.map (fun x -> (x.GetParameters().Length, x))
            |> List.fold (fun (widest, constructorList) (parameterCount, c) ->
                if parameterCount > widest then
                    (parameterCount, [ c ])
                elif parameterCount = widest then
                    (parameterCount, c :: constructorList)
                else
                    (widest, constructorList)
            ) (-1, [])

        let createObject (c: ConstructorInfo) =
            let resolveArgument (arg: ParameterInfo) =
                resolveInstance arg.ParameterType (t::typeStack)

            c.GetParameters()
            |> Array.map resolveArgument
            |> c.Invoke

        match widestConstructors with
        | [] -> raise (InvalidOperationException(sprintf "The type does not have any public constructor: %O" t))
        | [ c ] -> c |> createObject
        | _ -> raise (InvalidOperationException(sprintf "Cannot decide which constructor to use. The type has multiple constructors with the same maximum number of parameters: %O" t))

    /// Gets type instance for specified type
    let getInstance (t:Type) =
        resolveInstance t []

    interface IInstanceProvider with
        member this.RegisterInstance (t: Type, instance: obj) =
            instances.Store t instance

    interface IServiceProvider with
        [<DebuggerStepThrough>]
        member this.GetService (t: Type) =
            getInstance t

    interface IDisposable with
        member this.Dispose() =
            (instances :> IDisposable).Dispose()
