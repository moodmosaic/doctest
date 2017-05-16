open System
open System.IO

if not (File.Exists "paket.exe") then
    let url =
        "https://github.com/fsprojects/Paket/releases/download/4.8.5/paket.exe"
    use web =
        new Net.WebClient ()
    let tmp =
        Path.GetTempFileName ()
    web.DownloadFile (url, tmp)
    File.Move (tmp, Path.GetFileName url)

#r "paket.exe"
Paket.Dependencies.Install """source https://nuget.org/api/v2
nuget FAKE
nuget Argu
nuget FSharp.Data
nuget FSharp.Compiler.Service
nuget System.Xml.Linq
nuget Unquote""";;

#r "packages/FAKE/tools/FakeLib.dll"
open Fake.FscHelper
open Fake

let Target =
    Fake.TargetHelper.Target

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
    [ "packages/Argu/lib/net40/Argu.dll"
      "packages/FSharp.Compiler.Service/lib/net45/FSharp.Compiler.Service.dll"
      "packages/FSharp.Core/lib/net45/FSharp.Core.dll"
      "packages/FSharp.Data/lib/net40/FSharp.Data.dll"
      "packages/System.Xml.Linq/lib/net20/System.Xml.Linq.dll"
      "packages/Unquote/lib/net45/Unquote.dll" ]

let referencedAssemblies =
    let buildTarget name =
        Path.Combine (__SOURCE_DIRECTORY__, name)
    references
    |> List.map buildTarget
    |> List.map (fun path ->
        if not <| File.Exists(path)
            then failwithf "File not found '%s'" path
        path)

Target "Compile"
    (fun _ ->
        [ "Doctest.fs" ]
        |> Compile [
            Out <| Path.Combine (outputPath, "doctest.exe")
            FscHelper.Target TargetType.Exe
            NoFramework
            References referencedAssemblies
        ])

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
