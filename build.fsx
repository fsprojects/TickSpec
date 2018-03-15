#r "packages/build/fake/tools/FakeLib.dll"
#r "packages/build/Chessie/lib/net40/Chessie.dll"
#r "packages/build/paket.core/lib/net45/Paket.Core.dll"
#r "System.Xml.Linq"

open Fake
open Fake.AssemblyInfoFile
open Fake.Testing

module Paket = 
    open Paket.Domain
    let findReferencesFor packageName = 
        __SOURCE_DIRECTORY__ </> "paket.dependencies" 
        |> Paket.Dependencies 
        |> fun d -> d.FindReferencesFor(Paket.Domain.MainGroup, packageName)

module AppVeyor = 
    let BuildNumber = environVarOrNone "APPVEYOR_BUILD_NUMBER"

module Xml = 
    open System.Xml.Linq
    let load x = XDocument.Load(x:string)
    let xns x = (x:XDocument).Root.Name.Namespace
    let xn2 n xn = (xn:XNamespace).GetName(n)
    let descendants n x = (x:XDocument).Descendants(n)
    let value x = (x:XElement).Value

module MsBuildProject = 
    open System
    open Xml
    let tryAssembly project = 
        let xdoc = Xml.load project 
        let xmlns = xdoc |> Xml.xns
        xdoc |> Xml.descendants (xmlns |> Xml.xn2 "AssemblyName") |> Seq.map Xml.value |> Seq.tryHead
        |> Option.bind (fun assemblyName -> 
            !! (directory project </> "bin" </> sprintf "**/%s.dll" assemblyName)
            |> Seq.tryHead)

module Analysis = 
    let findAssembliesReferencing packageName = 
        Paket.findReferencesFor packageName
        |> Seq.collect (MsBuildProject.tryAssembly >> Option.toList)

open AppVeyor
open Fake.Testing.NUnit3

module ReleaseNotes = 
    let TickSpec = ReleaseNotesHelper.LoadReleaseNotes (__SOURCE_DIRECTORY__ </> "RELEASE_NOTES.md")

module Build = 
    let rootDir = __SOURCE_DIRECTORY__
    let nuget = rootDir </> "packed_nugets"
    let setParams defaults =
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

Target "Clean" <| fun _ -> DeleteDir Build.nuget
    
Target "AssemblyInfo" <| fun _ ->
    !! ("TickSpec" </> "AssemblyInfo.fs")
    |> Seq.iter(fun asmInfo ->
        let fileVersion =
            BuildNumber |> Option.defaultValue "0" 
            |> sprintf "%s.%s" ReleaseNotes.TickSpec.AssemblyVersion
        [ Attribute.Version fileVersion
          Attribute.FileVersion fileVersion ]
        |> AssemblyInfoFile.UpdateAttributes asmInfo)

Target "Build" <| fun _ -> build Build.setParams Sln

Target "Test" <| fun _ ->
    Analysis.findAssembliesReferencing "nunit"
    |> NUnit3 (fun p -> 
        { p with 
            ToolPath = Build.rootDir </> "packages/build/NUnit.ConsoleRunner/tools/nunit3-console.exe" })
    
    Analysis.findAssembliesReferencing "xunit"
    |> XUnit2.xUnit2 (fun p -> 
        { p with 
            ToolPath = Build.rootDir </> "packages/build/xunit.runner.console/tools/net452/xunit.console.exe"
            ForceAppVeyor = AppVeyor.BuildNumber |> Option.isSome })

Target "Nuget" <| fun _ -> 
    Paket.Pack (fun p ->
        { p with
            BuildPlatform = "AnyCPU"
            MinimumFromLockFile = true
            Symbols = true
            OutputPath = Build.nuget 
            ReleaseNotes = String.concat System.Environment.NewLine ReleaseNotes.TickSpec.Notes
            Version = ReleaseNotes.TickSpec.NugetVersion } )

Target "Publish" <| fun _ ->
    
    !! (Build.nuget </> "*.nupkg")
    -- (Build.nuget </> "*.symbols.nupkg")
    |> Seq.iter DeleteFile

    Paket.Push (fun p -> 
        { p with
            WorkingDir = Build.nuget })

Target "All" DoNothing

"AssemblyInfo"
        ==> "Build"
        ==> "Test"
        ==> "Nuget"
        ==> "All"

RunTargetOrDefault "All"