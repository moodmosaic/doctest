open Argu
open FSharp.Data
open Microsoft.FSharp.Compiler.Interactive.Shell
open System
open System.IO
open System.Reflection

[<assembly: AssemblyTitle       ("doctest")
; assembly: AssemblyDescription ("Test interactive F# examples, similar to doctest for Haskell.")
; assembly: AssemblyVersion     ("0.0.1")>]

do ()

type private Args =
    | [<MainCommand>]
      AsmPath of asmPath : string
    | DocPath of docPath : string
    | FsiPath of fsiPath : string
with
    interface IArgParserTemplate with
        member x.Usage =
            match x with
            | AsmPath _ -> "path of the assembly containing doctests."
            | DocPath _ -> "path of the assembly's XML documentation."
            | FsiPath _ -> "path of the F# Interactive (fsi/fsharpi)."

type private XmlDoc =
    XmlProvider<
       """<?xml version="1.0" encoding="UTF-8"?>
           <doc>
               <assembly>
                   <name>Foo</name>
               </assembly>
               <members>
                   <member name="P:Foo.Bar">
                       <summary>Quux</summary>
                   </member>
                   <member name="P:Foo.Bar">
                       <summary>Quux</summary>
                   </member>
               </members>
           </doc>""">

let private getDefaultFsi () =
    // http://www.mono-project.com/docs/faq/technical/
    // #how-to-detect-the-execution-platform
    match int Environment.OSVersion.Platform with
    | 4 | 128 -> "fshapri" // Linux
    | 6       -> "fsharpi" // macOS
    | _       -> "fsi"     // WinNT

let private guessDocsPath asmPath =
    Path.ChangeExtension (asmPath, ".XML")

let private unquotePath =
    let combine path1 path2 =
        Path.Combine (path2, path1) 
    Path.GetDirectoryName (
        Assembly.GetExecutingAssembly().Location)
    |> combine "Unquote.dll"

module ParseRun =
    let setup (x : string) (fsi : FsiEvaluationSession) =
        x.Split ([| '\n' |])
        |> Array.filter (fun x -> x.Contains ">>>")
        |> Array.iter   (fun x ->
               fsi.EvalInteraction <|
                   x.Replace(">>>", "").TrimStart([| ' ' |]).TrimEnd([| ' ' |]))

    let tests (x : string) (fsi : FsiEvaluationSession) = 
        let xs =
            x.Split([| '\n' |])
        
        let mutable result = true
        xs
        |> Array.indexed
        |> Array.filter (fun (_, x) -> x.Contains ">>>")
        |> Array.iter   (fun     x  ->
               let i = fst x
               let x = snd x

               let expected =
                   xs.[i + 1].TrimStart([| ' ' |])
               let actual =
                   x.Replace(">>>", "").TrimStart([| ' ' |]).TrimEnd([| ' ' |])
               
               let test =
                   expected + " = " + "(" + actual + ")"
               
               let passed =
                   (Option.get <|
                       fsi.EvalExpression test).ReflectionValue :?> bool
               if not passed then
                   printfn "%s;;" <| test.Replace ("\"", "")
                   fsi.EvalExpression
                       <| expected + " =! " + "(" + actual + ")"
                       |> ignore
                   result <- false)
        result

[<EntryPoint>]
let main (argv : string []) : int =
    let pExiter =
        ProcessExiter (
            colorizer =
                function ErrorCode.HelpText -> None | _ -> Some ConsoleColor.Red)
    let cliArgs =
        (ArgumentParser.Create<Args> (
              programName  = "doctest.exe"
            , errorHandler = pExiter)).ParseCommandLine argv
    
    let asmPath =
        cliArgs.GetResult (
              <@ AsmPath @>)

    let docPath =
        cliArgs.GetResult (
              <@ DocPath @>
            , defaultValue = guessDocsPath asmPath)

    let fsiPath =
        cliArgs.GetResult (
              <@ FsiPath @>
            , defaultValue = getDefaultFsi ())

    let session =
        FsiEvaluationSession.Create (
              FsiEvaluationSession.GetDefaultConfiguration ()
            , [| fsiPath
                 "--noninteractive" |]
              // Read input from the given reader.
            , new StringReader ""
              // Write output to the given writer.
            , new StringWriter (new System.Text.StringBuilder ())
              // Write errors to the given writer.
            , Console.Error
            )

    session.EvalInteraction <|
        sprintf """#r @"%s";;""" asmPath
    
    let xmlDoc =
        XmlDoc.Load docPath

    session.EvalInteraction <|
        sprintf "open %s;;" xmlDoc.Assembly.Name

    session.EvalInteraction <|
        sprintf """#r @"%s";;""" unquotePath

    session.EvalInteraction <|
        sprintf "open %s;;" "Swensen.Unquote"
    
    let setup =
        xmlDoc.Members
        |> Array.map    (fun x -> x.Summary)
        |> Array.filter (fun x -> x.Contains "$setup")

    let tests =
        xmlDoc.Members
        |> Array.map    (fun x -> x.Summary)
        |> Array.filter (fun x -> x.Contains ">>>")
        |> Array.except setup

    let runSetup x =
        ParseRun.setup x session
    let runTests x =
        ParseRun.tests x session

    // System Error Codes taken from 
    // http://msdn.microsoft.com/en-us/library/windows/desktop/ms681382.aspx
    let ERROR_SUCCESS =
        0
    let ERROR_INVALID_FUNCTION =
        1

    let setupResults =
        setup |> Array.map runSetup
    let testsResults =
        tests |> Array.map runTests

    if  testsResults |> Array.contains false then
        ERROR_INVALID_FUNCTION
    else
        ERROR_SUCCESS
