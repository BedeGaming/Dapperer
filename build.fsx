#r "./packages/FAKE/tools/FakeLib.dll"

let sd = __SOURCE_DIRECTORY__

open Fake
open Fake.DotNetCli
open Fake.GitVersionHelper

// --------------------------------------------------------------------------------------
// Build variables
// --------------------------------------------------------------------------------------

let gitVersion = GitVersion (id)
printfn "Current Version %s" gitVersion.NuGetVersion

let buildDir = sd @@ "build"

let buildOutDir = sd @@ "build" @@ "release"

let deployOutDir = buildDir @@ "dist"
let testResultsDir = buildDir @@ "test-results"

// --------------------------------------------------------------------------------------
// Helpers
// --------------------------------------------------------------------------------------

let test project =

    let setParams (p:TestParams) =
        { p with
            Project = project
            AdditionalArgs = 
                [   "--logger=trx"
                    "--no-build"
                    "--no-restore" 
                    "--results-directory=" + testResultsDir ]
        }
    DotNetCli.Test setParams

// --------------------------------------------------------------------------------------
// Targets
// --------------------------------------------------------------------------------------

Target "Clean" (fun () -> 
    CleanDirs [ buildOutDir; deployOutDir; testResultsDir ]
)

Target "Restore" (fun _ ->
    let setParams (p:RestoreParams) = { p with NoCache = true }
    DotNetCli.Restore setParams
)

Target "Build" (fun () ->

    let setParams (p:BuildParams) =
        { p with
            Project = "./Dapperer.sln"
            Configuration = "Release"
            AdditionalArgs = 
                [   "--no-incremental"
                    "--no-restore"
                    "--verbosity minimal"
                    "/p:Version=" + gitVersion.AssemblySemVer
                    "/p:DebugSymbols=True"
                    "/p:Optimize=True" ]
        }

    DotNetCli.Build setParams
)

Target "Test" (fun () ->

    !! "**/Dapperer.Tests.Unit.csproj"
    |> Seq.iter test
)

// --------------------------------------------------------------------------------------
// Build order
// --------------------------------------------------------------------------------------

"Clean"
    ==> "Restore"
    ==> "Build"
    ==> "Test"

RunTargetOrDefault "Test"