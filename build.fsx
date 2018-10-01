// --------------------------------------------------------------------------------------
// FAKE build script
// --------------------------------------------------------------------------------------

#r "paket: groupref FakeBuild //"

#load "./.fake/build.fsx/intellisense.fsx"

open Fake.Core
open Fake.Core.TargetOperators
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open Fake.DotNet
open Fake.DotNet.Testing
open System.Linq

//module Paket = 
//    let findReferencesFor packageName = 
//        __SOURCE_DIRECTORY__ </> "paket.dependencies" 
//        |> Paket.Dependencies
//        |> fun d -> d.FindReferencesFor(Paket.Domain.MainGroup, packageName)

module AppVeyor = 
    let BuildNumber = Environment.environVarOrNone "APPVEYOR_BUILD_NUMBER"

module Xml = 
    open System.Xml.Linq
    let load x = XDocument.Load(x:string)
    let xns x = (x:XDocument).Root.Name.Namespace
    let xn2 n xn = (xn:XNamespace).GetName(n)
    let descendants n x = (x:XDocument).Descendants(n)
    let value x = (x:XElement).Value

//module MsBuildProject = 
//    open System

//    let tryAssembly project = 
//        let xdoc = Xml.load project 
//        let xmlns = xdoc |> Xml.xns
//        let assemblyName = 
//            xdoc |> Xml.descendants (xmlns |> Xml.xn2 "AssemblyName") |> Seq.map Xml.value |> Seq.tryHead
//            |> Option.defaultValue (project.Replace(".fsproj","").Split( [| '\\'; '/' |]).Last())

//        !! (Path.getDirectory project </> "bin" </> sprintf "**/%s.dll" assemblyName)
//        |> Seq.tryHead

//module Analysis = 
//    let findAssembliesReferencing packageName = 
//        Paket.findReferencesFor packageName
//        |> Seq.collect (MsBuildProject.tryAssembly >> Option.toList)

module Test = 
    module NUnit = 
        ()
        //let assemblies () = Analysis.findAssembliesReferencing "NUnit" 
        //let run assemblies = 
        //    assemblies
        //    |> NUnit3.run (fun p -> { p with 
        //                                ToolPath = __SOURCE_DIRECTORY__ </> "packages/build/NUnit.ConsoleRunner/tools/nunit3-console.exe" })
    module XUnit = 
        ()
        //let assemblies () = Analysis.findAssembliesReferencing "xunit"  

        //let run assemblies = 
        //    assemblies
        //    |> XUnit2.run (fun p -> { p with 
        //                                ToolPath = __SOURCE_DIRECTORY__ </> "packages/build/xunit.runner.console/tools/net452/xunit.console.exe"
        //                                ForceAppVeyor = AppVeyor.BuildNumber |> Option.isSome })

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
    //NUnit.assemblies () |> NUnit.run
    //XUnit.assemblies () |> XUnit.run
    ()
)

Target.create "Nuget" (fun _ ->
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