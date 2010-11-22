namespace TicTacToe.Test

open System.Windows
open Microsoft.Silverlight.Testing
open Microsoft.Silverlight.Testing.Harness
open Microsoft.Silverlight.Testing.UnitTesting.Metadata
open TickSpec

type App() as app =
    inherit Application()    
   
    let initialize () =
        let settings = UnitTestSettings()
        let harness = UnitTestHarness()
        settings.TestHarness <- harness
        harness.Settings <- settings
        harness.Initialize()
        harness.TestRunStarting.Subscribe(fun ex ->
            let provider = TestProvider()
            let filter = TagTestRunFilter(settings, harness, settings.TagExpression)
            let features = GetFeatures(typeof<App>.Assembly)
            for feature in features do
                provider.RegisterFeature feature
                let provider = provider :> IUnitTestProvider
                let assembly = provider.GetUnitTestAssembly(harness,feature.Assembly)
                harness.EnqueueTestAssembly(assembly,filter)
        ) |> ignore
        settings

    do  app.Startup.Add(fun _ -> 
        let settings = initialize()
        app.RootVisual <- UnitTestSystem.CreateTestPage(settings)
    )