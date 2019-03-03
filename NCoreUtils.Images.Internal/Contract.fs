namespace NCoreUtils.Images

open Microsoft.AspNetCore.Http

type IImageSourceExtractor =
  abstract AsyncExtractImageSource : httpContext:HttpContext -> Async<IImageSource voption>

type IImageDestinationExtractor =
  abstract AsyncExtractImageDestination : httpContext:HttpContext -> Async<IImageDestination voption>