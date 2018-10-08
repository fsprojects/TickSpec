#load "./.fake/build.fsx/intellisense.fsx"
open Fake.Core
open Fake.Core.TargetOperators
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open Fake.DotNet
open Fake.DotNet.Testing

module Paket = 
    let findReferencesFor packageName = 
        __SOURCE_DIRECTORY__ </> "paket.dependencies" 
        |> Paket.Dependencies
        |> fun d -> d.FindReferencesFor(Paket.Domain.MainGroup, packageName)

module AppVeyor = 
    let BuildNumber = Environment.environVarOrNone "APPVEYOR_BUILD_NUMBER"

module Xml = 
    open System.Xml.Linq
    let load x = XDocument.Load(x:string)
    let xns x = (x:XDocument).Root.Name.Namespace
    let xn2 n xn = (xn:XNamespace).GetName(n)
    let descendants n x = (x:XDocument).Descendants(n)
    let value x = (x:XElement).Value

module Analysis = 
    let findAssembliesReferencing packageName = 
        Paket.findReferencesFor packageName

module Test = 
    module NUnit = 
        ()
        let projects () = Analysis.findAssembliesReferencing "NUnit"
        let run projects = 
            projects
            |> Seq.iter (fun p -> DotNet.test (fun o -> 
                {o with Configuration = DotNet.BuildConfiguration.Release; 
                        Framework = if Environment.isWindows then None else Some "netcoreapp2.1"}) p)
    module XUnit = 
        ()
        let projects () = Analysis.findAssembliesReferencing "xunit"

        let run projects = 
            if Environment.isWindows then
                projects
                |> Seq.iter (fun (p:string) -> 
                    // currently support for custom container is broken when IL generator isn't used, so we are skipping the .NET Core test
                    DotNet.test (fun o -> {o with Configuration = DotNet.BuildConfiguration.Release;
                                                  Framework = if p.EndsWith("CustomContainer.fsproj") then Some "net452" else None }) p)

open AppVeyor
open Test

module ReleaseNotes = 

    let TickSpec = ReleaseNotes.load (__SOURCE_DIRECTORY__ </> "RELEASE_NOTES.md")

module Build = 
    let rootDir = __SOURCE_DIRECTORY__
    let nuget = rootDir </> "packed_nugets"
    let setParams (defaults :MSBuildParams) =
        { defaults with
            Verbosity = Some MSBuildVerbosity.Normal
            Targets = ["Rebuild"]
            Properties =
                [ "AllowedReferenceRelatedFileExtensions", ".pdb"
                  "Optimize", "True"
                  "DebugSymbols", "True"
                  "Configuration", "Release" ]
         }

let Sln = "./TickSpec.sln"

Target.create "Clean" (fun _ ->
    Shell.cleanDirs [Build.nuget]
)

Target.create "AssemblyInfo" (fun _ ->
    !! ("TickSpec" </> "AssemblyInfo.fs")
    |> Seq.iter(fun asmInfo ->
        let fileVersion =
            BuildNumber |> Option.defaultValue "0" 
            |> sprintf "%s.%s" ReleaseNotes.TickSpec.AssemblyVersion
        [ AssemblyInfo.Version fileVersion
          AssemblyInfo.FileVersion fileVersion ]
        |> AssemblyInfoFile.updateAttributes asmInfo)
)

Target.create "Build" (fun _ ->
    MSBuild.build Build.setParams Sln
)

Target.create "Test" (fun _ ->
    NUnit.projects () |> NUnit.run
    XUnit.projects () |> XUnit.run
)

Target.create "Nuget" (fun _ ->
    if Environment.isWindows then
        DotNet.pack (fun p ->
            { p with
                Configuration = DotNet.Release
                OutputPath = Some Build.nuget} )
            "TickSpec\TickSpec.fsproj"
)

Target.create "Publish" (fun _ ->
    !! (Build.nuget </> "*.nupkg")
    -- (Build.nuget </> "*.symbols.nupkg")
    |> Seq.iter File.delete

    Paket.push (fun p -> 
        { p with
            WorkingDir = Build.nuget })
)

Target.create "All" ignore

"AssemblyInfo"
        ==> "Build"
        ==> "Test"
        ==> "Nuget"
        ==> "All"

Target.runOrDefault "All"