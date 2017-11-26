namespace TickSpec

open System
open System.Collections.Generic
open System.Diagnostics
open System.Reflection

/// Provides an instance provider for tests
type IInstanceProvider =
    inherit IServiceProvider

    /// Registers an instance for a type (if there is already a registered instance, it will be replaced)
    abstract member RegisterInstance : Type * obj -> unit

/// <summary>
/// Creates wrapper for <see cref="IServiceProvider" /> so it can be used in TickSpec scenarios.
/// </summary>
/// <param name="innerProvider">The provider to be wrapped</param>
type ServiceProviderWrapper(innerProvider: IServiceProvider) as self =
    /// Type instances for invoked steps
    let instances = Dictionary<_,_>()

    let getInstance (t:Type) =
        match t with
        | t when t = typeof<IInstanceProvider> -> self :> obj
        | t ->
            match instances.TryGetValue t with
            | true, instance -> instance
            | false, _ -> innerProvider.GetService(t)

    interface IServiceProvider with
        [<DebuggerStepThrough>]
        member this.GetService (t: Type) =
            getInstance t

    interface IInstanceProvider with
        member this.RegisterInstance (t: Type, instance: obj) =
            match instances.TryGetValue t with
            | true, value ->
                match value with
                | :? IDisposable as d -> d.Dispose()
                | _ -> ()

                instances.[t] <- instance
            | _ -> instances.Add(t, instance)

            instances.[t] <- instance

    interface IDisposable with
        member this.Dispose() =
            instances.Values
            |> Seq.iter (function
                | :? IDisposable as d -> d.Dispose()
                | _ -> ())

            instances.Clear()

            match innerProvider with
            | :? IDisposable as d -> d.Dispose()
            | _ -> ()

[<Sealed>]
/// Creates instance service provider
type InstanceProvider() as self =
    /// Type instances for invoked steps
    let instances = Dictionary<_,_>()

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
            | true, instance -> instance
            | false, _ ->
                let instance = createInstance t typeStack
                instances.Add(t, instance)
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
            match instances.TryGetValue t with
            | true, value ->
                match value with
                | :? IDisposable as d -> d.Dispose()
                | _ -> ()

                instances.[t] <- instance
            | _ -> instances.Add(t, instance)

            instances.[t] <- instance

    interface IServiceProvider with
        [<DebuggerStepThrough>]
        member this.GetService (t: Type) =
            getInstance t

    interface IDisposable with
        member this.Dispose() =
            instances.Values
            |> Seq.iter (function
                | :? IDisposable as d -> d.Dispose()
                | _ -> ())

            instances.Clear()