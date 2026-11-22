/// Runs test code on the GUI thread, as WxKit.Tests' Gui does, and keeps the application alive
/// between tests with a hidden frame.
module WxKit.FSharp.Tests.Gui

open System
open System.Threading.Tasks
open WxKit

let private failures = Collections.Generic.List<string>()

let mutable private keeper: Frame | null = null

let start () =
    keeper <- new Frame(null, "WxKit F# tests")
    Application.UnhandledException.Add(fun e ->
        lock failures (fun () -> failures.Add(e.Exception.ToString()))
        e.Handled <- true)
    Application.AssertFailed.Add(fun e ->
        lock failures (fun () -> failures.Add(e.ToString()))
        e.Handled <- true)

let stop () =
    Application.Post(fun () ->
        match keeper with
        | null -> ()
        | frame -> frame.Destroy() |> ignore
        Application.Exit())

let private takeFailures () =
    lock failures (fun () ->
        let taken = String.Join("\n", failures)
        failures.Clear()
        taken)

/// Runs the body with a new frame on the GUI thread, and destroys the frame afterwards. An
/// exception from an event handler, or a failed wxWidgets assertion, fails the test.
let withFrame (body: Frame -> unit) : Task =
    let completion = TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously)
    Application.Post(fun () ->
        takeFailures () |> ignore
        let frame = new Frame(null, "Test frame")
        try
            try
                body frame
                match takeFailures () with
                | "" -> completion.SetResult()
                | failed -> completion.SetException(InvalidOperationException failed)
            with error ->
                completion.SetException error
        finally
            if not frame.IsDestroyed then frame.Destroy() |> ignore)
    completion.Task
