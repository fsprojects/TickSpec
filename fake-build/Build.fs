open Fake.Core
open Fake.Core.TargetOperators
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open Fake.DotNet

let initializeContext () =
    let execContext = Context.FakeExecutionContext.Create false "build.fsx" []
    Context.setExecutionContext (Context.RuntimeContext.Fake execContext)

initializeContext ()

module GitHubActions =
    let BuildNumber = Environment.environVarOrNone "BUILD_NUMBER"
    let NugetVersion = Environment.environVarOrNone "NUGET_VERSION"
    let NugetKey = Environment.environVarOrNone "NUGET_KEY"

    let detect() = BuildNumber |> Option.isSome

module Xml =
    open System.Xml.Linq
    let load x = XDocument.Load(x:string)
    let xns x = (x:XDocument).Root.Name.Namespace
    let xn2 n xn = (xn:XNamespace).GetName(n)
    let descendants n x = (x:XDocument).Descendants(n)
    let value x = (x:XElement).Value

module Analysis =
    let projectReferencing packageName project =
        let projDoc = Xml.load project
        let ns = projDoc |> Xml.xns
        let packageRefName = ns |> Xml.xn2 "PackageReference"
        let includeName = ns |> Xml.xn2 "Include"
        let pRefs =
            projDoc
            |> Xml.descendants packageRefName
            |> Seq.filter (fun x -> not (isNull (x.Attribute(includeName))))
            |> Seq.map (fun x -> x.Attribute(includeName).Value)

        if pRefs |> Seq.contains packageName then Some project
        else None

module ReleaseNotes =
    let TickSpec = ReleaseNotes.load (__SOURCE_DIRECTORY__ </> ".." </> "RELEASE_NOTES.md")

module Build =
    let rootDir = __SOURCE_DIRECTORY__ </> ".."
    let nuget = rootDir </> "packed_nugets"

    let private fileVersion =
        GitHubActions.BuildNumber
        |> Option.defaultValue "0"
        |> sprintf "%s.%s" ReleaseNotes.TickSpec.AssemblyVersion

    let private continuousBuild =
        if GitHubActions.detect() then
            "/p:ContinuousIntegrationBuild=true"
        else
            ""

    let props =
            sprintf "/p:Version=%s /p:AssemblyVersion=%s %s" ReleaseNotes.TickSpec.AssemblyVersion fileVersion continuousBuild

    let setParams (defaults: DotNet.BuildOptions) =
        { defaults with
            Configuration = DotNet.BuildConfiguration.Release
            MSBuildParams = { defaults.MSBuildParams with DisableInternalBinLog = true }
            Common =
                DotNet.Options.Create()
                |> DotNet.Options.withCustomParams (Some props) }

let Sln = Path.combine Build.rootDir "TickSpec.sln"

Target.create "Clean" (fun _ ->
    Shell.cleanDirs [Build.nuget]
    DotNet.exec id "clean" "" |> ignore
)

Target.create "Build" (fun _ ->
    Sln |> DotNet.build Build.setParams
)

Target.create "Test" (fun _ ->
    // Xunit seems to be failing under Linux with net452 runner, let's just skip it
    // the .NET 4 tests all together there
    let framework = if Environment.isWindows then None else Some "net9.0"

    Sln
    |> DotNet.test (fun o ->
        { o with
            Configuration = DotNet.Release
            NoBuild = true
            MSBuildParams = { o.MSBuildParams with DisableInternalBinLog = true }
            Framework = framework
        }
    )
)

Target.create "Nuget" (fun _ ->
    if Environment.isWindows then
        let props =
            let notes =
                String.concat System.Environment.NewLine ReleaseNotes.TickSpec.Notes
                |> (fun x -> x.Replace(",", "%2c"))

            sprintf
                "%s /p:PackageReleaseNotes=\"%s\";PackageVersion=\"%s\""
                Build.props
                notes
                ReleaseNotes.TickSpec.NugetVersion

        DotNet.pack (fun p ->
            { p with
                Configuration = DotNet.Release
                OutputPath = Some Build.nuget
                NoBuild = false // Not sure why but it seems to be necessary to rebuild it
                IncludeSymbols = true
                MSBuildParams = { p.MSBuildParams with DisableInternalBinLog = true }
                Common =
                    DotNet.Options.Create()
                    |> DotNet.Options.withCustomParams (Some props)
                    |> DotNet.Options.withVerbosity (Some DotNet.Verbosity.Minimal)
            } )
            Sln
    else
        Trace.tracef "--- Skipping Nuget target as the build is not running on Windows ---"
)

Target.create "PublishNuget" (fun _ ->
    if Environment.isWindows then
        match GitHubActions.NugetKey with
        | Some k -> TraceSecrets.register k "<NUGET_KEY>"
        | None -> ()

        let publishNugets () =
            let key =
                match GitHubActions.NugetKey with
                | Some x -> x
                | None -> failwith "To publish nuget, it is needed to set NUGET_KEY environment variable"

            let publishNuget nuget =
                DotNet.exec id "nuget" (sprintf "push %s -k %s -s https://api.nuget.org/v3/index.json" nuget key)

            !! (Build.nuget </> "*.nupkg")
            -- (Build.nuget </> "*.symbols.nupkg")
            |> Seq.map publishNuget

        match GitHubActions.NugetVersion with
        | None -> ()
        | Some t when t = ReleaseNotes.TickSpec.NugetVersion ->
            publishNugets () |> Seq.iter (fun x -> if not x.OK then failwithf "Nuget publish failed with %A" x)
        | Some t -> failwithf "Unexpected tag %s" t
    else
        Trace.tracef "--- Skipping PublishNuget target as the build is not running on Windows ---"
)

let dependencies = [
    "Clean" ==> "Build" ==> "Nuget" ==> "Test"
    "Test" ==> "PublishNuget"
]

[<EntryPoint>]
let main args =
    Target.runOrDefaultWithArguments ("Test")
    0
