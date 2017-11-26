module ByFeature.CustomContainer.Feature

open Autofac
open System
open System.Collections.Generic
open TickSpec
open Xunit

/// Adapts the Autofac container to fulfill the TickSpec IInstanceProvider interface
type InstanceProvider(container : IContainer) =
    let cache = Dictionary<Type,obj>()
    interface IDisposable with
        member __.Dispose() =
            // TODO Dispose items remaining in cache in reverse order
            container.Dispose()
    interface IInstanceProvider with
        member __.RegisterInstance(serviceType : Type, instance : obj) =
            // TODO handle disposal
            cache.[serviceType] <- instance
    interface IServiceProvider with
        member __.GetService(serviceType) =
            match cache.TryGetValue serviceType with
            | true, instance -> instance
            | _ -> container.Resolve(serviceType)

/// Creates an Instance Provider to be used for a single Scenario run
let createInstanceProvider : unit -> IInstanceProvider =
    let concreteTypesSource = Features.ResolveAnything.AnyConcreteTypeNotAlreadyRegisteredSource()
    let builder = new ContainerBuilder()
    builder.RegisterSource concreteTypesSource
    let container = builder.Build()
    // NB as instances cannot leak across parallel scenario runs, we need to generate an independent instance provider per scenario run in order to handle the case where two Scenarios from the same Test Assembly but different xUnit Test Classes run in parallel
    fun () -> new InstanceProvider(container) :> _

let source = AssemblyStepDefinitionsSource(System.Reflection.Assembly.GetExecutingAssembly(), createInstanceProvider)
let scenarios resourceName = source.ScenariosFromEmbeddedResource resourceName |> MemberData.ofScenarios

[<Theory; MemberData("scenarios", "Shelter.feature")>]
let Shelter(scenario : Scenario) = scenario.Action.Invoke()