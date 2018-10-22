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
            |> Seq.filter (fun x -> x.Attribute(includeName) <> null)
            |> Seq.map (fun x -> x.Attribute(includeName).Value)
        
        if pRefs |> Seq.contains packageName then Some project
        else None

module ReleaseNotes = 
    let TickSpec = ReleaseNotes.load (__SOURCE_DIRECTORY__ </> "RELEASE_NOTES.md")

module Build = 
    let rootDir = __SOURCE_DIRECTORY__
    let nuget = rootDir </> "packed_nugets"
    let setParams (defaults :DotNet.BuildOptions) =
        { defaults with Configuration = DotNet.BuildConfiguration.Release }

module Test =
    let private runTests filter framework =
        !! "**/*.fsproj"
        ++ "**/*.csproj"
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
            let framework = if Environment.isWindows then None else Some "netcoreapp2.1"
            runTests "Xunit" framework

let Sln = "./TickSpec.sln"

Target.create "Clean" (fun _ ->
    Shell.cleanDirs [Build.nuget]
)

Target.create "AssemblyInfo" (fun _ ->
    !! ("TickSpec" </> "AssemblyInfo.fs")
    |> Seq.iter(fun asmInfo ->
        let fileVersion =
            AppVeyor.BuildNumber |> Option.defaultValue "0" 
            |> sprintf "%s.%s" ReleaseNotes.TickSpec.AssemblyVersion
        [ AssemblyInfo.Version fileVersion
          AssemblyInfo.FileVersion fileVersion ]
        |> AssemblyInfoFile.updateAttributes asmInfo)
)

Target.create "Build" (fun _ ->
    Sln |> DotNet.build Build.setParams
)

Target.create "Test" (fun _ ->
    Test.NUnit.run()
    Test.XUnit.run()
)

Target.create "Nuget" (fun _ ->
    if Environment.isWindows then
        let props = 
            let notes = String.concat System.Environment.NewLine ReleaseNotes.TickSpec.Notes
            "--include-symbols /p:" + "PackageReleaseNotes=\"" + notes + "\";PackageVersion=\"" + ReleaseNotes.TickSpec.NugetVersion + "\""
        DotNet.pack (fun p ->
            { p with
                Configuration = DotNet.Release
                OutputPath = Some Build.nuget
                Common = 
                    DotNet.Options.Create()
                    |> DotNet.Options.withCustomParams (Some props)
            } )
            "TickSpec\\TickSpec.fsproj"
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