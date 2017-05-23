open System
open System.IO
open System.Reflection
open Argu
open FSharp.Data
open Microsoft.FSharp.Compiler.Interactive.Shell

[<assembly: AssemblyTitle       ("Doctest")
; assembly: AssemblyDescription ("An implementation of Haskell Doctest for F#.")
; assembly: AssemblyVersion     ("0.0.5")>]
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

module Runner =
    let setup (x : string) (fsi : FsiEvaluationSession) =
        x.Split ([| '\n' |])
        |> Array.filter (fun x -> x.Contains ">>>")
        |> Array.iter   (fun x ->
               fsi.EvalInteraction <|
                   x.Replace(">>>", "").TrimStart([| ' ' |]).TrimEnd([| ' ' |]))

    let tests (x : string) (fsi : FsiEvaluationSession) = 
        let xs =
            x.Split([| '\n' |])
        
        let result = ref true
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
                   printf "%s" <| expected + " = " + actual
                   fsi.EvalExpression
                       <| expected + " =! " + "(" + actual + ")"
                       |> ignore
                   result := false)
        !result

[<EntryPoint>]
let main (argv : string []) : int =
    let pExiter =
        ProcessExiter (
            colorizer =
                function ErrorCode.HelpText -> None | _ -> Some ConsoleColor.Red)
    let cliArgs =
        (ArgumentParser.Create<Args> (
              programName  = "Doctest"
            , errorHandler = pExiter)).ParseCommandLine argv
    
    let asmPath =
        cliArgs.GetResult (
              <@ AsmPath @>)

    let docPath =
        cliArgs.GetResult (
              <@ DocPath @>
            , defaultValue =
                  Path.ChangeExtension (asmPath, ".XML"))

    let fsiPath =
        cliArgs.GetResult (
              <@ FsiPath @>
            , defaultValue =
                  // http://www.mono-project.com/docs/faq/technical/
                  // #how-to-detect-the-execution-platform
                  match int Environment.OSVersion.Platform with
                  | 4 | 128 -> "fshapri"
                  | 6       -> "fsharpi"
                  | _       -> "fsi")

    let fsiSession =
        FsiEvaluationSession.Create (
              FsiEvaluationSession.GetDefaultConfiguration ()
            , [| fsiPath
                 "--noninteractive" |]
              // Read input from the given reader.
            , new StringReader ""
              // Write output to the given writer.
            , new StringWriter (System.Text.StringBuilder ())
              // Write errors to the given writer.
            , Console.Error
            )

    fsiSession.EvalInteraction <|
        sprintf """#r @"%s";;""" asmPath
    
    let xmlDoc =
        XmlDoc.Load docPath

    fsiSession.EvalInteraction <|
        sprintf "open %s;;" xmlDoc.Assembly.Name

    let unquotePath =
        let combine path1 path2 =
            Path.Combine (path2, path1)
        Path.GetDirectoryName (
            System.Reflection.Assembly.GetExecutingAssembly().Location)
        |> combine "Unquote.dll"

    fsiSession.EvalInteraction <|
        sprintf """#r @"%s";;""" unquotePath

    fsiSession.EvalInteraction <|
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

    let setupResults =
        setup
        |> Array.map (fun x -> fsiSession |> Runner.setup x)

    let testsResults =
        tests
        |> Array.map (fun x -> fsiSession |> Runner.tests x)

    if  testsResults |> Array.forall ((=) true) then
        0 // Success
    else
        1 // Failure
