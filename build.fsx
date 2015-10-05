#r @"src/packages/FAKE/tools/FakeLib.dll"
open System.IO
open Fake
open Fake.AssemblyInfoFile
open Fake.Git.Information
open Fake.SemVerHelper

let buildOutputPath = "./build_output"
let buildArtifactPath = "./build_artifacts"
let nugetWorkingPath = FullName "./build_temp"
let packagesPath = FullName "./src/packages"
let keyFile = FullName "./MassTransit.snk"

let assemblyVersion = "3.0.0.0"
let baseVersion = "3.0.13"

let semVersion : SemVerInfo = parse baseVersion

let Version = semVersion.ToString()

let branch = (fun _ ->
  (environVarOrDefault "APPVEYOR_REPO_BRANCH" (getBranchName "."))
)

let FileVersion = (environVarOrDefault "APPVEYOR_BUILD_VERSION" (Version + "." + "0"))

let informationalVersion = (fun _ ->
  let branchName = (branch ".")
  let label = if branchName="master" then "" else " (" + branchName + "/" + (getCurrentSHA1 ".").[0..7] + ")"
  (FileVersion + label)
)

let nugetVersion = (fun _ ->
  let branchName = (branch ".")
  let label = if branchName="master" then "" else "-" + (if branchName="mt3" then "beta" else branchName)
  (Version + label)
)

let InfoVersion = informationalVersion()
let NuGetVersion = nugetVersion()


printfn "Using version: %s" Version

Target "Clean" (fun _ ->
  ensureDirectory buildOutputPath
  ensureDirectory buildArtifactPath
  ensureDirectory nugetWorkingPath

  CleanDir buildOutputPath
  CleanDir buildArtifactPath
  CleanDir nugetWorkingPath
)

Target "RestorePackages" (fun _ ->
     "./src/MassTransit.Persistence.RavenDB.sln"
     |> RestoreMSSolutionPackages (fun p ->
         { p with
             OutputPath = packagesPath
             Retries = 4 })
)

Target "Build" (fun _ ->

  CreateCSharpAssemblyInfo @".\src\SolutionVersion.cs"
    [ Attribute.Title "MassTransit"
      Attribute.Description "MassTransit is a distributed application framework for .NET http://masstransit-project.com"
      Attribute.Product "MassTransit"
      Attribute.Version assemblyVersion
      Attribute.FileVersion FileVersion
      Attribute.InformationalVersion InfoVersion
    ]

  let buildMode = getBuildParamOrDefault "buildMode" "Release"
  let setParams defaults = {
    defaults with
        Verbosity = Some(Quiet)
        Targets = ["Clean"; "Build"]
        Properties =
            [
                "Optimize", "True"
                "DebugSymbols", "True"
                "RestorePackages", "True"
                "Configuration", buildMode
                "SignAssembly", "True"
                "AssemblyOriginatorKeyFile", keyFile
                "TargetFrameworkVersion", "v4.5"
                "Platform", "Any CPU"
            ]
  }

  build setParams @".\src\MassTransit.Persistence.RavenDB.sln"
      |> DoNothing
)

let testDlls = [ "./src/MassTransit.Persistence.RavenDB.Tests/bin/Release/MassTransit.Persistence.RavenDB.Tests.dll" ]

Target "UnitTests" (fun _ ->
    testDlls
        |> NUnit (fun p ->
            {p with
                Framework = "v4.0.30319"
                DisableShadowCopy = true;
                OutputFile = buildArtifactPath + "/nunit-test-results.xml"})
)

type packageInfo = {
    Project: string
    PackageFile: string
    Summary: string
    Files: list<string*string option*string option>
}

Target "Package" (fun _ ->

  let nugs = [| { Project = "MassTransit.Persistence.RavenDB"
                  Summary = "MassTransit RavenDB Saga Transport"
                  PackageFile = @".\src\MassTransit.Persistence.RavenDB\packages.config"
                  Files = [ (@"..\src\MassTransit.Persistence.RavenDB\bin\Release\MassTransit.Persistence.RavenDB.*", Some @"lib\net45", None);
                            (@"..\src\MassTransit.Persistence.RavenDB\**\*.cs", Some "src", None) ] }
             |]

  nugs
    |> Array.iter (fun nug ->

      let getDeps daNug : NugetDependencies =
        if daNug.Project = "MassTransit.Persistence.RavenDB" then (getDependencies daNug.PackageFile)
        else ("MassTransit.Persistence.RavenDB", NuGetVersion) :: (getDependencies daNug.PackageFile)

      let setParams defaults = {
        defaults with
          Authors = ["Chris Patterson"; "Dru Sellers"; "Travis Smith" ]
          Description = "MassTransit RavenDB Saga Transport."
          OutputPath = buildArtifactPath
          Project = nug.Project
          Dependencies = (getDeps nug)
          Summary = nug.Summary
          SymbolPackage = NugetSymbolPackage.Nuspec
          Version = NuGetVersion
          WorkingDir = nugetWorkingPath
          Files = nug.Files
      }

      NuGet setParams (FullName "./template.nuspec")
    )
)

Target "Default" (fun _ ->
  trace "Build starting..."
)

"Clean"
  ==> "RestorePackages"
  ==> "Build"
  ==> "UnitTests"
  ==> "Package"
  ==> "Default"

RunTargetOrDefault "Default"
