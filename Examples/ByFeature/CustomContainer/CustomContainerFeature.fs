module ByFeature.CustomContainer.Feature

open Autofac
open TickSpec
open Xunit

// Autofac customizations specific to this test Suite
type DomainModule() =
    inherit Module()
    override __.Load builder =
        // Special case this to ensure it only gets created/Disposed once
        builder.RegisterType<Domain.DogRun>().SingleInstance() |> ignore

type Shelter(container : AutofacFixture) =
    static let source = AssemblyStepDefinitionsSource(System.Reflection.Assembly.GetExecutingAssembly())
    do source.ServiceProviderFactory <- container.CreateScopedServiceProvider
    // When actually running the tests, wire in the link to the container so creation of the Step Definition and Domain types gets hooked correctly
    static let scenarios resourceName = source.ScenariosFromEmbeddedResource resourceName |> MemberData.ofScenarios
    [<Theory; MemberData("scenarios", "CustomContainer.Shelter.feature")>]
    let run(scenario : Scenario) = scenario.Action.Invoke()
    // Indicate our interest in having xUnit manage the ContainerFixture for us (notably Disposing at the end of a test run)
    interface IClassFixture<AutofacFixture>