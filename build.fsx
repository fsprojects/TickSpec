#r "paket:
    nuget NUnit.ConsoleRunner
    nuget xunit.runner.console
    nuget Fake.Core.Target
    nuget Fake.IO.FileSystem
    nuget Fake.DotNet.Cli
    nuget Fake.Tools.Git
    nuget Fake.DotNet.MSBuild
    nuget Fake.Core.ReleaseNotes 
    nuget Fake.DotNet.AssemblyInfoFile
    nuget Fake.DotNet.Paket
    nuget Fake.DotNet.Testing.XUnit2
    nuget Fake.DotNet.Testing.NUnit
    nuget Fake.Api.GitHub
    nuget Paket.Core //"

#load "./.fake/build.fsx/intellisense.fsx"

open Fake.Core
open Fake.Core.TargetOperators
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open Fake.DotNet

module AppVeyor = 
    let BuildNumber = Environment.environVarOrNone "APPVEYOR_BUILD_NUMBER"
    let Tag = Environment.environVarOrNone "APPVEYOR_REPO_TAG_NAME"
    let NugetKey = Environment.environVarOrNone "NUGET_KEY"

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
    let TickSpec = ReleaseNotes.load (__SOURCE_DIRECTORY__ </> "RELEASE_NOTES.md")

module Build = 
    let rootDir = __SOURCE_DIRECTORY__
    let nuget = rootDir </> "packed_nugets"
    let setParams (defaults :DotNet.BuildOptions) =
        let fileVersion =
            AppVeyor.BuildNumber |> Option.defaultValue "0" 
            |> sprintf "%s.%s" ReleaseNotes.TickSpec.AssemblyVersion

        let props =
            sprintf "/p:Version=%s /p:AssemblyVersion=%s" ReleaseNotes.TickSpec.AssemblyVersion fileVersion

        { defaults with 
            Configuration = DotNet.BuildConfiguration.Release 
            Common =
                DotNet.Options.Create()
                |> DotNet.Options.withCustomParams (Some props) }

module Test =
    let private runTests filter framework =
        !! "**/*.fsproj"
        ++ "**/*.csproj"
        -- "Wiring/*.fsproj"
        |> Seq.choose (Analysis.projectReferencing filter)
        |> Seq.iter (fun p -> p |> DotNet.test (fun o ->
            { o with 
                Configuration = DotNet.BuildConfiguration.Release
                NoBuild = true
                Framework = framework}))
            
    module NUnit =
        let run () = runTests "NUnit" None

    module XUnit = 
        let run () = 
            let framework = if Environment.isWindows then None else Some "net5.0"
            runTests "Xunit" framework

    module MSTest =
       let run () =
           runTests "MSTest.TestFramework" None

    module Expecto =
        let run () =
            runTests "Expecto" None

let Sln = "./TickSpec.sln"

Target.create "Clean" (fun _ ->
    Shell.cleanDirs [Build.nuget]
)

Target.create "Build" (fun _ ->
    Sln |> DotNet.build Build.setParams
)

Target.create "Test" (fun _ ->
    Test.NUnit.run()
    Test.XUnit.run()
    Test.MSTest.run()
    Test.Expecto.run()
)

Target.create "Nuget" (fun _ ->
    if Environment.isWindows then
        let props = 
            let notes =
                String.concat System.Environment.NewLine ReleaseNotes.TickSpec.Notes
                |> (fun x -> x.Replace(",", "%2c"))

            "--no-build --include-symbols /p:" + "PackageReleaseNotes=\"" + notes + "\";PackageVersion=\"" + ReleaseNotes.TickSpec.NugetVersion + "\""
        DotNet.pack (fun p ->
            { p with
                Configuration = DotNet.Release
                OutputPath = Some Build.nuget
                Common = 
                    DotNet.Options.Create()
                    |> DotNet.Options.withCustomParams (Some props)
            } )
            "TickSpec\\TickSpec.fsproj"
    else
        Trace.tracef "--- Skipping Nuget target as the build is not running on Windows ---"   
)

Target.create "PublishNuget" (fun _ ->
    if Environment.isWindows then
        match AppVeyor.NugetKey with
        | Some k -> TraceSecrets.register k "<NUGET_KEY>"
        | None -> ()

        let publishNugets () =
            let key = 
                match AppVeyor.NugetKey with
                | Some x -> x
                | None -> failwith "To publish nuget, it is needed to set NUGET_KEY environment variable"        

            let publishNuget nuget =
                DotNet.exec id "nuget" (sprintf "push %s -k %s -s https://api.nuget.org/v3/index.json" nuget key)
        
            !! (Build.nuget </> "*.nupkg")
            -- (Build.nuget </> "*.symbols.nupkg")
            |> Seq.map publishNuget

        match AppVeyor.Tag with
        | None -> ()
        | Some t when t = ReleaseNotes.TickSpec.NugetVersion ->
            publishNugets () |> Seq.iter (fun x -> if not x.OK then failwithf "Nuget publish failed with %A" x)
        | Some t -> failwithf "Unexpected tag %s" t
    else
        Trace.tracef "--- Skipping PublishNuget target as the build is not running on Windows ---"   
)

Target.create "All" ignore

"Clean"
    ==> "Build"
    ==> "Test"
    ==> "Nuget"
    ==> "PublishNuget"
    ==> "All"

Target.runOrDefault "All"