// wxWidgets gets the main thread, as macOS needs, and xUnit's runner a thread of its own, as in
// WxKit.Tests' Program.cs.
module WxKit.FSharp.Tests.Program

open System.Threading
open WxKit
open Xunit.MicrosoftTestingPlatform
open Xunit.Runner.InProc.SystemConsole

// The testing platform's extensions, which its generated SelfRegisteredExtensions adds, but which F#
// compiles after this file.
let private addExtensions (builder: Microsoft.Testing.Platform.Builder.ITestApplicationBuilder) (args: string array) =
    Microsoft.Testing.Platform.MSBuild.TestingPlatformBuilderHook.AddExtensions(builder, args)
    Microsoft.Testing.Extensions.Telemetry.TestingPlatformBuilderHook.AddExtensions(builder, args)

let private runTests (args: string array) =
    if args |> Array.exists (fun arg -> arg = "--server" || arg = "--internal-msbuild-node") then
        TestPlatformTestFramework.RunAsync(args, System.Action<_, _>(addExtensions)).GetAwaiter().GetResult()
    else
        ConsoleRunner.Run(args).GetAwaiter().GetResult()

[<EntryPoint>]
let main args =
    let mutable result = 1
    let exitCode =
        Application.Run(fun () ->
            Gui.start ()
            let runner =
                Thread(
                    (fun () ->
                        try
                            result <- runTests args
                        finally
                            Gui.stop ()),
                    Name = "xUnit runner"
                )
            runner.Start())
    if result <> 0 then result else exitCode
