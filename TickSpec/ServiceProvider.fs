namespace TickSpec

open System
open System.Collections.Generic
open System.Diagnostics
open System.Reflection

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
    /// Registered type mappings
    let typeRegistrations = Dictionary<_,_>()

    /// Type instances for invoked steps
    let instances = Dictionary<_,_>()
    /// Resolves an instance
    let rec resolveInstance (t:Type) (typeStack: Type list) =
        let resolvingType = 
            match typeRegistrations.TryGetValue t with
            | true, target -> target
            | false, _ -> t

        let alreadyRequested =
            typeStack
            |> List.tryFind (fun x -> x = resolvingType)

        match alreadyRequested with
        | Some _ -> raise (InvalidOperationException(sprintf "Circular dependency found when resolving type %O" resolvingType))
        | None -> ()

        match resolvingType with
        | resolvingType when resolvingType = typeof<IInstanceProvider> -> self :> obj
        | resolvingType ->
            match instances.TryGetValue resolvingType with
            | true, instance -> instance
            | false, _ ->
                let instance = createInstance resolvingType typeStack
                instances.Add(resolvingType, instance)
                instance

    /// Creates an instance if there was none
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
            typeRegistrations.Add(typeof<'TInterface>, typeof<'TType>)

        [<DebuggerStepThrough>]
        member this.RegisterInstanceAs<'TInterface> (instance:'TInterface) =
            instances.Add(typeof<'TInterface>, instance)
    interface System.IServiceProvider with
        [<DebuggerStepThrough>]
        member this.GetService(t:Type) =
            getInstance t
