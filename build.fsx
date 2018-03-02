// include Fake libs
#r "./packages/FAKE/tools/FakeLib.dll"

open Fake

// Directories
let deployDir = "./deploy/"
let buildDir = "./build/"

// Targets
Target "Clean" <| fun _ ->
    CleanDirs [buildDir; deployDir]

Target "Build" <| fun _ ->
    MSBuildDebug (buildDir)  "Build" !!"src/Shinobus.fsproj"
    |> Log "AppBuild-Output: "

// Build order
"Clean"
  ==> "Build"

// start build
RunTargetOrDefault "Build"
