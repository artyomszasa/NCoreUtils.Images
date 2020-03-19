namespace NCoreUtils.Images.GoogleCloudStorage

open System.Text.Json.Serialization

[<CLIMutable>]
[<NoEquality; NoComparison>]
type GoogleObjectData = {
  [<property: JsonPropertyName("cacheControl")>]
  CacheControl : string
  [<property: JsonPropertyName("contentType")>]
  ContentType  : string
  [<property: JsonPropertyName("name")>]
  Name         : string }