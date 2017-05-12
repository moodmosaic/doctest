open System
open System.IO

if not (File.Exists "paket.exe") then
    let url =
        "https://github.com/fsprojects/Paket/releases/download/0.26.3/paket.exe"
    use web =
        new Net.WebClient ()
    let tmp =
        Path.GetTempFileName ()
    web.DownloadFile (url, tmp)
    File.Move (tmp, Path.GetFileName url)

#r "paket.exe"

Paket.Dependencies.Install """source https://nuget.org/api/v2
nuget FAKE 3.14.9
""";;

#r "packages/FAKE/tools/FakeLib.dll"
open Fake
open Fake.FscHelper

let outputPath =
    Path.Combine (__SOURCE_DIRECTORY__, "bin")

Target "CreateOutputPath"
    (fun _ ->
        CreateDir
            outputPath)

Target "CleanOutputPath"
    (fun _ ->
        CleanDir
            outputPath)

let references =
    []

Target "Compile"
    (fun _ ->
        [ "Program.fs" ]
        |> Fsc
            (fun opts ->
               { opts with
                   References = references
                   FscTarget  = Exe
                   Output     = Path.Combine (outputPath, "doctest.exe") }))

Target "CopyAssemblyReferences"
    (fun _ ->
        CopyFiles
            outputPath
            references)

"CreateOutputPath"
    ==> "CleanOutputPath"
    ==> "Compile"
    ==> "CopyAssemblyReferences"

RunTargetOrDefault "CopyAssemblyReferences"
