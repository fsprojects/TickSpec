module ByFeature.CustomContainer.Feature

open Autofac
open System
open System.Collections.Generic
open TickSpec
open Xunit

/// Creates a IServiceProvider to be used for a single Scenario run
let createInstanceProvider : unit -> IServiceProvider =
    let concreteTypesSource = Features.ResolveAnything.AnyConcreteTypeNotAlreadyRegisteredSource()
    let builder = new ContainerBuilder()
    builder.RegisterSource concreteTypesSource
    let container = builder.Build()

    fun () ->
        let scope = container.BeginLifetimeScope();
        { new obj()
            interface IServiceProvider with member __.GetService(serviceType) = scope.Resolve(serviceType)
            interface IDisposable with member __.Dispose() = scope.Dispose() }

let source = AssemblyStepDefinitionsSource(System.Reflection.Assembly.GetExecutingAssembly(), createInstanceProvider)
let scenarios resourceName = source.ScenariosFromEmbeddedResource resourceName |> MemberData.ofScenarios

[<Theory; MemberData("scenarios", "Shelter.feature")>]
let Shelter(scenario : Scenario) = scenario.Action.Invoke()