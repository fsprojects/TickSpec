namespace TickSpec

open TickSpec
open System.Reflection
open System.Collections.Generic
open Microsoft.Silverlight.Testing
open Microsoft.Silverlight.Testing.Harness
open Microsoft.Silverlight.Testing.UnitTesting.Metadata

type TestProvider () =
    let capabilities = 
        UnitTestProviderCapabilities.MethodCanIgnore |||
        UnitTestProviderCapabilities.MethodCanDescribe
    let featureLookup = Dictionary<Assembly,Feature>()
    let assemblyLookup = Dictionary<Assembly,IAssembly>()
    member this.RegisterFeature(feature) =
        featureLookup.Add(feature.Assembly,feature)
    interface IUnitTestProvider with
        member this.Capabilities = capabilities
        member this.GetUnitTestAssembly(testHarness:UnitTestHarness,assemblyRef:Assembly) =
            match assemblyLookup.TryGetValue(assemblyRef) with
            | true, assembly -> assembly
            | false, _ ->
                let feature = featureLookup.[assemblyRef]
                let assembly = new TestAssembly(this,testHarness,feature)
                assemblyLookup.[assemblyRef] <- assembly
                assembly :> IAssembly
        member this.HasCapability capability = capability &&& capabilities = capability
        member this.IsFailedAssert ex =
            let t = ex.GetType()
            let t' = typeof<Microsoft.VisualStudio.TestTools.UnitTesting.AssertFailedException>
            t = t' || t.IsSubclassOf t'
        member this.Name = "TickSpec"
and TestAssembly (provider:IUnitTestProvider,harness:UnitTestHarness,feature:Feature) as this =
    let classes = new List<ITestClass>();
    do  classes.Add(new TestClass(this, feature));
    interface IAssembly with
        member this.AssemblyCleanupMethod = null
        member this.AssemblyInitializeMethod = null
        member this.GetTestClasses() = classes :> ICollection<ITestClass>
        member this.Provider = provider
        member this.TestHarness = harness
        member this.Name = feature.Source
and TestClass (assembly:IAssembly,feature:Feature) =
    interface ITestClass with
        member this.Assembly = assembly
#if SILVERLIGHT_TOOLKIT_DECEMBER_2011
        member this.Namespace = assembly.Name
#endif
        member this.ClassCleanupMethod = null
        member this.ClassInitializeMethod = null
        member this.GetTestMethods () =
            let methods = List<ITestMethod>()
            for scenario in feature.Scenarios do
                TestMethod(feature,scenario) |> methods.Add  
            methods :> ICollection<ITestMethod>
        member this.Ignore = false
        member this.Name = feature.Name
        member this.TestCleanupMethod = null
        member this.TestInitializeMethod = null
        member this.Type = typeof<obj>
and TestMethod (feature:Feature,scenario:Scenario) =
    let ignore = scenario.Tags |> Array.exists ((=) "ignore")
    let e = DelegateEvent<_>()
    interface ITestMethod with
        member this.Category = null
        member this.DecorateInstance(instance:obj) = ()
        member this.Description = scenario.Description
        member this.ExpectedException = null
        member this.GetDynamicAttributes() =
            seq {
                yield feature.Source
                yield feature.Name
                yield scenario.Name
                for tag in scenario.Tags do yield tag
            }
            |> Seq.map (fun name -> 
                let name = 
                    name
                        .Replace("(","_")
                        .Replace(")","_")
                        .Replace("-","_")
                TagAttribute(name)
            ) 
            |> Seq.cast
        member this.Ignore = ignore
        member this.Invoke(instance) = scenario.Action.Invoke()
        member this.Method = scenario.Action.Method
        member this.Name = scenario.ToString()
        member this.Owner = feature.Name
        member this.Priority = Microsoft.Silverlight.Testing.UnitTesting.Metadata.VisualStudio.Priority(3) :> IPriority
        member this.Properties = List<ITestProperty>() :> ICollection<ITestProperty>
        member this.Timeout = System.Nullable<int>()
        member this.WorkItems = null
        [<CLIEvent>]
        member this.WriteLine =  e.Publish
