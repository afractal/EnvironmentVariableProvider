// --------------------------------------------------------------------------------------
// FAKE build script
// --------------------------------------------------------------------------------------

#r @"packages/build/FAKE/tools/FakeLib.dll"
open Fake
open Fake.Git
open Fake.AssemblyInfoFile
open Fake.ReleaseNotesHelper
open Fake.UserInputHelper

open System
open System.IO
open System.Diagnostics

// --------------------------------------------------------------------------------------
// START TODO: Provide project-specific details below
// --------------------------------------------------------------------------------------

// Information about the project are used
//  - for version and project name in generated AssemblyInfo file
//  - by the generated NuGet package
//  - to run tests and to publish documentation on GitHub gh-pages
//  - for documentation, you also need to edit info in "docsrc/tools/generate.fsx"

// The name of the project
// (used by attributes in AssemblyInfo, name of a NuGet package and directory in 'src')
let project = "EnvironmentVariableProvider"

// Short summary of the project
// (used as description in AssemblyInfo and as a short summary for NuGet package)
let summary = "Type provider for getting environment variables."

// Longer description of the project
// (used as a description for NuGet package; line breaks are automatically cleaned up)
let description = "Type provider for getting environment variables."

// List of author names (for NuGet package)
let authors = [ "afractal" ]

// Tags for your project (for NuGet package)
let tags = "environment variable type provider"

// File system information
let solutionFile  = "EnvironmentVariableProvider.sln"

// Default target configuration
let configuration = "Release"

// Pattern specifying assemblies to be tested using NUnit
let testAssemblies = "tests/**/bin" </> configuration </> "*Tests*.dll"

// Git configuration (used for publishing documentation in gh-pages branch)
// The profile where the project is posted
let gitOwner = "afractal"
let gitHome = "https://github.com/" + gitOwner

//  sprintf "%s/%s" "https://github.com/afractal/EnvironmentVariableProvider" gitOwner

// The name of the project on GitHub
let gitName = "EnvironmentVariableProvider"

// The url for the raw files hosted
let gitRaw = environVarOrDefault "gitRaw" "https://raw.githubusercontent.com/afractal"

// --------------------------------------------------------------------------------------
// END TODO: The rest of the file includes standard build steps
// --------------------------------------------------------------------------------------

// Read additional information from the release notes document
let release = LoadReleaseNotes "RELEASE_NOTES.md"

// Helper active pattern for project types
let (|Fsproj|Csproj|Vbproj|Shproj|) (projFileName:string) =
    match projFileName with
    | f when f.EndsWith("fsproj") -> Fsproj
    | f when f.EndsWith("csproj") -> Csproj
    | f when f.EndsWith("vbproj") -> Vbproj
    | f when f.EndsWith("shproj") -> Shproj
    | _                           -> failwith (sprintf "Project file %s not supported. Unknown project type." projFileName)

// Generate assembly info files with the right version & up-to-date information
Target "AssemblyInfo" (fun _ ->
    let getAssemblyInfoAttributes projectName =
        [ Attribute.Title (projectName)
          Attribute.Product project
          Attribute.Description summary
          Attribute.Version release.AssemblyVersion
          Attribute.FileVersion release.AssemblyVersion
          Attribute.Configuration configuration ]

    let getProjectDetails projectPath =
        let projectName = System.IO.Path.GetFileNameWithoutExtension(projectPath)
        ( projectPath,
          projectName,
          System.IO.Path.GetDirectoryName(projectPath),
          (getAssemblyInfoAttributes projectName)
        )

    !! "src/**/*.??proj"
    |> Seq.map getProjectDetails
    |> Seq.iter (fun (projFileName, projectName, folderName, attributes) ->
        match projFileName with
        | Fsproj -> CreateFSharpAssemblyInfo (folderName </> "AssemblyInfo.fs") attributes
        | Csproj -> CreateCSharpAssemblyInfo ((folderName </> "Properties") </> "AssemblyInfo.cs") attributes
        | Vbproj -> CreateVisualBasicAssemblyInfo ((folderName </> "My Project") </> "AssemblyInfo.vb") attributes
        | Shproj -> ()
        )
)

// Copies binaries from default VS location to expected bin folder
// But keeps a subdirectory structure for each project in the
// src folder to support multiple project outputs
Target "CopyBinaries" (fun _ ->
    !! "src/**/*.??proj"
    -- "src/**/*.shproj"
    |>  Seq.map (fun f -> ((System.IO.Path.GetDirectoryName f) </> "bin" </> configuration, "bin" </> (System.IO.Path.GetFileNameWithoutExtension f)))
    |>  Seq.iter (fun (fromDir, toDir) -> CopyDir toDir fromDir (fun _ -> true))
)

// --------------------------------------------------------------------------------------
// Clean build results

let vsProjProps =
#if MONO
    [ ("DefineConstants","MONO"); ("Configuration", configuration) ]
#else
    [ ("Configuration", configuration); ("Platform", "Any CPU") ]
#endif

Target "Clean" (fun _ ->
    !! solutionFile |> MSBuildReleaseExt "" vsProjProps "Clean" |> ignore
    CleanDirs ["bin"; "temp"; "docs"]
)

// --------------------------------------------------------------------------------------
// Build library


Target "Build" (fun _ ->
    !! solutionFile
    |> MSBuildReleaseExt "" vsProjProps "Rebuild"
    |> ignore
)

// --------------------------------------------------------------------------------------
// Build a NuGet package

Target "NuGet" (fun _ ->

    CopyDir @"temp/lib" "bin" allFiles

    NuGet (fun p ->
        { p with
            Authors = authors
            Project = project
            Summary = summary
            Description = description
            Version = release.NugetVersion
            ReleaseNotes = String.Join(Environment.NewLine, release.Notes)
            Tags = tags
            WorkingDir = "temp"
            OutputPath = "bin"
            AccessKey = getBuildParamOrDefault "nugetkey" ""
            Publish = hasBuildParam "nugetkey"
            Dependencies = [] })
        (project + ".nuspec")

    CleanDir "Temp"
    // Branches.tag "" release.NugetVersion
    // Paket.Pack(fun p ->
    //     { p with
    //         OutputPath = "bin"
    //         Version = release.NugetVersion
    //         ReleaseNotes = toLines release.Notes
    //         ProjectUrl = "https://github.com/afractal/EnvironmentVariableProvider" })
)

Target "PublishNuget" (fun _ ->
     Paket.Pack(fun p ->
        { p with
            Version = release.NugetVersion
            ReleaseNotes = String.Join(Environment.NewLine, release.Notes)
            Symbols = true
            OutputPath = "bin" })
)

// --------------------------------------------------------------------------------------
// Release Scripts

// #load "paket-files/build/fsharp/FAKE/modules/Octokit/Octokit.fsx"
// open Octokit

// Target "Release" (fun _ ->
//     StageAll ""
//     Git.Commit.Commit "" (sprintf "Bump version to %s" release.NugetVersion)
//     Branches.push ""

//     Branches.tag "" release.NugetVersion
//     Branches.pushTag "" "origin" release.NugetVersion

//     // release on github
//     createClient (getBuildParamOrDefault "github-user" "") (getBuildParamOrDefault "github-pw" "")
//     |> createDraft gitOwner gitName release.NugetVersion (release.SemVer.PreRelease <> None) release.Notes
//     // TODO: |> uploadFile "PATH_TO_FILE"
//     |> releaseDraft
//     |> Async.RunSynchronously


    // let user =
    //     match getBuildParam "github-user" with
    //     | s when not (String.IsNullOrWhiteSpace s) -> s
    //     | _ -> getUserInput "Username: "
    // let pw =
    //     match getBuildParam "github-pw" with
    //     | s when not (String.IsNullOrWhiteSpace s) -> s
    //     | _ -> getUserPassword "Password: "
    // let remote =
    //     Git.CommandHelper.getGitResult "" "remote -v"
    //     |> Seq.filter (fun (s: string) -> s.EndsWith("(push)"))
    //     |> Seq.tryFind (fun (s: string) -> s.Contains(gitOwner + "/" + gitName))
    //     |> function None -> gitHome + "/" + gitName | Some (s: string) -> s.Split().[0]

    // StageAll ""
    // Git.Commit.Commit "" (sprintf "Bump version to %s" release.NugetVersion)
    // Branches.pushBranch "" remote (Information.getBranchName "")

    // Branches.tag "" release.NugetVersion
    // Branches.pushTag "" remote release.NugetVersion

    // // release on github
    // createClient user pw
    // |> createDraft gitOwner gitName release.NugetVersion (release.SemVer.PreRelease <> None) release.Notes
    // // TODO: |> uploadFile "PATH_TO_FILE"
    // |> releaseDraft
    // |> Async.RunSynchronously
// )

Target "BuildPackage" DoNothing

// --------------------------------------------------------------------------------------
// Run all targets by default. Invoke 'build <Target>' to override

Target "All" DoNothing

"AssemblyInfo"
  ==> "Build"
  ==> "CopyBinaries"
  ==> "NuGet"
  ==> "BuildPackage"
  ==> "All"

// "Clean"
//   ==> "Release"

"BuildPackage"
  ==> "PublishNuget"
//   ==> "Release"

RunTargetOrDefault "All"
