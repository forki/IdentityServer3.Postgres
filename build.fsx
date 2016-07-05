// Author: Jos van der Til <jos@vandertil.eu>

#r "packages/FAKE/tools/FakeLib.dll"

open Fake

// Common directories
let rootDir = FileUtils.pwd()
let sourceDir = rootDir @@ "src"
let buildDir = rootDir @@ "build"

Target "Clean" (fun _ ->
    // Does what it says on the tin.
    CleanDir buildDir 
)

Target "Build" (fun _ ->
    // Find all solution files in the source folder or any subdirectory
    let solutions = !!(sourceDir @@ "**" @@ "*.sln")

    // Build all solutions, result is a list of file paths.
    let outputFiles = solutions |> MSBuildRelease outputDir "Build"

    // We do not use these file paths for anything, so just discard them.
    outputFiles |> ignore
)

Target "Test" (fun _ ->
    !!(buildDir @@ "*.Tests.dll")
    |> FixieHelper.Fixie id
)

"Clean" ==> "Build" ==> "Test"

RunTargetOrDefault "Build"
