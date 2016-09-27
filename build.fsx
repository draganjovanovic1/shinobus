// include Fake libs
#r "./packages/FAKE/tools/FakeLib.dll"

open Fake

// Directories
let buildDir  = "./build/"
let deployDir = "./deploy/"

let getBuildDir name = buildDir + name

// Filesets
let appReferences  =
    !! "/**/*.csproj"
      ++ "/**/*.fsproj"

let projects =
    [("Shinobus", "src/Shinobus.fsproj")]

// version info
let version = "0.1"  // or retrieve from CI server

// Targets
Target "Clean" (fun _ ->
    projects
    |> List.iter (fun (name, path) ->
        CleanDirs [getBuildDir name; deployDir]
    )
)

Target "Build" (fun _ ->
    // compile all projects below src/app/
    projects
    |> List.iter (fun (name, path) ->
        MSBuildDebug (getBuildDir name)  "Build" !!path
        |> Log "AppBuild-Output: "
    )
)

// Build order
"Clean"
  ==> "Build"
  
// start build
RunTargetOrDefault "Build"
