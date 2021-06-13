namespace TickSpec

module AutofacHelpers =
    open Autofac

    let createContainer () =
        let builder = new ContainerBuilder()
        let concreteTypesSource = Features.ResolveAnything.AnyConcreteTypeNotAlreadyRegisteredSource()
        builder.RegisterSource concreteTypesSource
        // Scan for Modules in this test assembly
        builder.RegisterAssemblyModules(typeof<Feature>) |> ignore
        builder.Build()

    let beginScopeAsServiceProvider (container : IContainer) : System.IServiceProvider =
        // Instances obtained from Autofac will be grouped/shared based on this scope, which is used for all steps of the Scenario
        // TickSpec will trigger the Disposal of items obtained via the scope via IDisposable
        let scope = container.BeginLifetimeScope()
        { new obj()
            // Provide standard IServiceProvider mechanism for obtainging instances
            interface System.IServiceProvider with
                member __.GetService(serviceType) =
                    scope.Resolve(serviceType)
            // Enable caller to clean up all items in the Scope
            interface System.IDisposable with
                member __.Dispose() =
                    scope.Dispose() }

/// Wrap Autofac container as an XUnit Fixture so it can be correctly Disposed at end of test run
type AutofacFixture() =
    let container = AutofacHelpers.createContainer ()
    interface System.IDisposable with
        member __.Dispose() =
            // Trigger the Disposal of any single instance items
            container.Dispose()
    member __.CreateScopedServiceProvider() : System.IServiceProvider =
        // Instances obtained from Autofac will be grouped/shared based on this scope, which is used for all steps of the Scenario
        // TickSpec will trigger the Disposal of items obtained via the scope via IDisposable
        AutofacHelpers.beginScopeAsServiceProvider container
