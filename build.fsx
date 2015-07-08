// Author: Jos van der Til <jos@vandertil.eu>

#r "build/FAKE/tools/FakeLib.dll"

open Fake

// Common directories
let rootDir = FileUtils.pwd()
let sourceDir = rootDir @@ "src"
let buildDir = rootDir @@ "build"
let outputDir = buildDir @@ "output"

Target "Clean" (fun _ ->
    // Does what it says on the tin.
    CleanDir outputDir
)

Target "Build" (fun _ ->
    // Find all solution files in the source folder or any subdirectory
    let solutions = !!(sourceDir @@ "**" @@ "*.sln")

    // Build all solutions, result is a list of file paths.
    let outputFiles = solutions |> MSBuildRelease outputDir "Build"

    // We do not use these file paths for anything, so just discard them.
    outputFiles |> ignore
)

"Clean" ==> "Build"

RunTargetOrDefault "Build"
