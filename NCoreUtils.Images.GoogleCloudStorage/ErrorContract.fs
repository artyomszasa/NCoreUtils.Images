namespace NCoreUtils.Images

open System
open System.Runtime.Serialization

[<Serializable>]
type GoogleCloudStorageAccessException =
  inherit Exception
  new (message) =
    { inherit Exception (message) }
  new (message, innerExn : exn) =
    { inherit Exception (message, innerExn) }
  new (info : SerializationInfo, context) =
    { inherit Exception (info, context) }

[<Serializable>]
type GoogleCloudStorageUploadException =
  inherit Exception
  new (message) =
    { inherit Exception (message) }
  new (message, innerExn : exn) =
    { inherit Exception (message, innerExn) }
  new (info : SerializationInfo, context) =
    { inherit Exception (info, context) }
