namespace NCoreUtils.Images

open System
open System.Collections
open System.Collections.Generic
open System.Collections.ObjectModel
open System.Runtime.InteropServices
open NCoreUtils
open NCoreUtils.Images.Internal

[<Interface>]
[<AllowNullLiteral>]
type IImageResizerOptions =
  abstract MemoryLimit : Nullable<int64> with get, set
  abstract Quality  : imageType:string -> int
  abstract Optimize : imageType:string -> bool

type ImageTypeDictionary<'value when 'value : equality> =

  val private def : 'value
  val mutable private ``default`` : 'value
  val private values : Dictionary<string, 'value>
  internal new (def) = { def = def; ``default`` = def; values = Dictionary () }

  member private this.TryValue key =
    let mutable res = Unchecked.defaultof<_>
    match this.values.TryGetValue (key, &res) with
    | true -> ValueSome res
    | _    -> ValueNone

  member internal this.Value imageType = this.TryValue imageType |> ValueOption.defaultValue this.``default``

  member private this.RawItem
    with get (key : string) = this.Value key
    and  set key value =
      match key with
      | EQI "default" -> this.``default`` <- value
      | imageType     -> this.values.[imageType] <- value
      | _ -> ()
  member private this.GetEnumerator () =
    let items = seq {
      yield KeyValuePair ("default", this.``default``)
      yield! this.values |> Seq.map (fun kv -> KeyValuePair (kv.Key.ToString (), kv.Value)) }
    items.GetEnumerator ()
  interface IEnumerable with
    member this.GetEnumerator () = this.GetEnumerator () :> _
  interface IEnumerable<KeyValuePair<string, 'value>> with
    member this.GetEnumerator () = this.GetEnumerator ()
  interface ICollection<KeyValuePair<string, 'value>> with
    member __.IsReadOnly = false
    member this.Count = this.values.Count + 1
    member this.Add kv = this.RawItem kv.Key <- kv.Value
    member this.Clear () =
      this.``default`` <- this.def
      this.values.Clear ()
    member this.Contains kv =
      match kv.Key with
      | EQI "default"  -> kv.Value = this.``default``
      | _              -> (this.values :> ICollection<_>).Contains kv
    member this.CopyTo (array, index) =
      array.[index] <- KeyValuePair ("default", this.``default``)
      let mutable i = index + 1
      for kv in this.values do
        array.[i] <- KeyValuePair (kv.Key.ToString (), kv.Value)
        i <- i + 1
    member this.Remove kv =
      match kv.Key with
      | EQI "default" -> if this.``default`` = kv.Value then this.``default`` <- this.def; true else false
      | _             -> (this.values :> ICollection<_>).Remove kv
  interface IDictionary<string, 'value> with
    member this.Item
      with get key = this.RawItem key
      and  set key value = this.RawItem key <- value
    member this.Keys =
      let list = ResizeArray ()
      list.Add "default"
      this.values.Keys |> Seq.map (fun k -> k.ToString ()) |> list.AddRange
      ReadOnlyCollection list :> _
    member this.Values =
      let list = ResizeArray ()
      list.Add this.``default``
      this.values.Values |> list.AddRange
      ReadOnlyCollection list :> _
    member this.Add (key, value) = this.RawItem key <- value
    member this.ContainsKey key =
      match key with
      | EQI "default" -> true
      | imageType     -> this.values.ContainsKey imageType
    member this.Remove key =
      match key with
      | EQI "default" -> this.``default`` <- this.def; true
      | imageType     -> this.values.Remove imageType
    member this.TryGetValue (key, [<Out>] value : byref<_>) =
      match key with
      | EQI "default" -> value <- this.``default``; true
      | imageType     -> this.values.TryGetValue (imageType, &value)

type ImageTypeQuality () = inherit ImageTypeDictionary<int> (85)

type ImageTypeOptimize () = inherit ImageTypeDictionary<bool> (false)


[<CLIMutable>]
[<NoEquality; NoComparison>]
type ImageResizerOptions = {
  MemoryLimit : Nullable<int64>
  Quality     : ImageTypeQuality
  Optimize    : ImageTypeOptimize }
  with
    static member Default = { Quality = ImageTypeQuality (); Optimize = ImageTypeOptimize (); MemoryLimit = Nullable<int64> (200L * 1024L * 1024L) }
    interface IImageResizerOptions with
      member this.MemoryLimit
        with get () = this.MemoryLimit
        and set (limit : Nullable<int64>) = typeof<ImageResizerOptions>.GetProperty("MemoryLimit").SetValue(this, limit, null)
      member this.Quality imageType =
        match box this.Quality with
        | null -> 85
        | _    -> this.Quality.Value imageType
      member this.Optimize imageType =
        match box this.Optimize with
        | null -> false
        | _    -> this.Optimize.Value imageType
