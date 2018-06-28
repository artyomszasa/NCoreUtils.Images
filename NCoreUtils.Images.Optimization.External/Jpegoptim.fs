namespace NCoreUtils.Images.Optimization

open System.Diagnostics
open System.IO
open System.Text
open NCoreUtils
open NCoreUtils.Images
open System

type JpegoptimOptimization (logger : ILog<JpegoptimOptimization>) =
  member __.AsyncResOptimize (data : byte[]) =
    let startInfo =
      ProcessStartInfo (
        FileName               = "jpegoptim",
        Arguments              = "-q --strip-all --all-progressive --stdin --stdout",
        RedirectStandardInput  = true,
        RedirectStandardOutput = true,
        RedirectStandardError  = true,
        CreateNoWindow         = true,
        UseShellExecute        = false)
    let p = new Process (StartInfo = startInfo)
    match p.Start () with
    | false ->
      ImageResizerError.optimizationFailedf "jpegoptim" "Failed to start process with [FileName = %s, Arguments = %s]" startInfo.FileName startInfo.Arguments
      |> Error
      |> async.Return
    | _ ->
      async {
        p.EnableRaisingEvents <- true
        let inline kill () = p.Kill ()
        logger.LogDebug (null, sprintf "Executing jpegoptim on source (%d bytes)" data.Length)
        let stopwatch = Stopwatch ()
        stopwatch.Start ()
        let! asyncOutput = async {
          use buffer = new MemoryStream ()
          do! Stream.asyncCopyTo buffer p.StandardOutput.BaseStream
          return buffer.ToArray () } |> Async.StartChildAsTask
        let! asyncError = async {
          use buffer = new MemoryStream ()
          do! Stream.asyncCopyTo buffer p.StandardError.BaseStream
          return buffer.ToArray () |> Encoding.UTF8.GetString } |> Async.StartChildAsTask
        let! asyncInput = async {
          use buffer = new MemoryStream (data, false)
          do! Stream.asyncCopyTo p.StandardInput.BaseStream buffer
          do! p.StandardInput.BaseStream.AsyncFlush ()
          p.StandardInput.BaseStream.Close () }  |> Async.StartChildAsTask
        do! Async.AwaitEvent (p.Exited, kill) |> Async.Ignore
        do! Async.AwaitTask asyncInput
        let! output = asyncOutput |> Async.AwaitTask
        let! error  = asyncError  |> Async.AwaitTask
        stopwatch.Stop ()
        match p.ExitCode with
        | 0 ->
          return
            match output.Length < data.Length with
            | true ->
              let oldSize = data.Length
              let newSize = output.Length
              let decrease = (1.0 - (float newSize / float oldSize)) * 100.0
              logger.LogDebug (null, sprintf "Successfully optimized image (jpegoptim, %d bytes -> %d bytes, %.2f%% decrease)" oldSize newSize decrease)
              Ok output
            | _ ->
              logger.LogDebug (null, "Unable to further optimize image (jpegoptim).")
              Ok data
        | exitCode ->
          return
            ImageResizerError.optimizationFailedf "jpegoptim" "Process exited with code %d, stderr = %s" exitCode error
            |> Error }
  member this.AsyncOptimize data = async {
    let! res = this.AsyncResOptimize data
    return
      match res with
      | Ok result -> result
      | Error err -> err.RaiseException () }
  interface IImageOptimization with
    member __.Supports imageType = StringComparer.OrdinalIgnoreCase.Equals ("jpg", imageType) || StringComparer.OrdinalIgnoreCase.Equals ("jpeg", imageType)
    member this.AsyncResOptimize data = this.AsyncResOptimize data
    member this.AsyncOptimize data = this.AsyncOptimize data

