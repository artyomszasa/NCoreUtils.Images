namespace NCoreUtils.Images

open NCoreUtils.AspNetCore
open NCoreUtils
open System.Text

module internal GCMiddleware =

  let private (|PathGC|_|) ci =
    match ci = CaseInsensitive "gc" with
    | true -> Some ()
    | _    -> None

  let run httpContext asyncNext =
    match HttpContext.path httpContext with
    | [ PathGC ] ->
      let before = System.GC.GetTotalMemory false
      System.Runtime.GCSettings.LargeObjectHeapCompactionMode <- System.Runtime.GCLargeObjectHeapCompactionMode.CompactOnce;
      System.GC.Collect ()
      System.GC.WaitForPendingFinalizers ()
      System.GC.Collect ()
      System.GC.WaitForPendingFinalizers ()
      let after = System.GC.GetTotalMemory false
      let message = sprintf "Forced garbage collection: %d -> %d bytes\n LatencyMode = %A" before after System.Runtime.GCSettings.LatencyMode
      let bytes = Encoding.UTF8.GetBytes message
      let response = HttpContext.response httpContext
      response.StatusCode <- 200
      response.ContentType <- "text/plain; charset=utf-8"
      response.ContentLength <- Nullable.mk bytes.LongLength
      async.Combine (response.Body.AsyncWrite bytes, response.Body.AsyncFlush ())
    | _ -> asyncNext
