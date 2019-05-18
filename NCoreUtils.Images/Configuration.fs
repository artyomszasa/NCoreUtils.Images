namespace NCoreUtils.Images.Configuration

open System
open System.Collections
open System.Collections.Generic
open System.Collections.Immutable
open System.Runtime.CompilerServices
open System.Runtime.InteropServices
open NCoreUtils
open NCoreUtils.Images
open NCoreUtils.Images.Providers
open NCoreUtils.Images.Resizers

[<NoEquality; NoComparison>]
type private ImageResizerOptions = {
  Quality  : ImmutableDictionary<string, int>
  Optimize : ImmutableDictionary<string, bool> }
  with
    interface IImageResizerOptions with
      member this.Quality imageType =
        let mutable q = Unchecked.defaultof<_>
        match this.Quality.TryGetValue (imageType, &q) with
        | true -> q
        | _    -> DefaultImageResizerOptions.Instance.Quality imageType
      member this.Optimize imageType =
        let mutable b = Unchecked.defaultof<_>
        match this.Optimize.TryGetValue (imageType, &b) with
        | true -> b
        | _    -> false


type ImageResizerOptionsBuilder () =
  let mutable qualities = ImmutableDictionary.Empty
  let mutable optimize  = ImmutableDictionary.Empty
  member __.Options = { Quality = qualities; Optimize = optimize } :> IImageResizerOptions
  member this.OptimizeByDefault imageType =
    optimize <- optimize.SetItem (imageType, true)
    this
  member this.DoNotOptimizeByDefault imageType =
    optimize <- optimize.SetItem (imageType, false)
    this
  member this.UseDefaultQuality (imageType, quality : int) =
    qualities <- qualities.SetItem (imageType, quality)
    this

type
  [<Sealed>]
  ResizerCollectionBuilder =
    val internal collection : Dictionary<string, IResizerFactory>
    new () = { collection = Dictionary StringComparer.OrdinalIgnoreCase }

    member this.Add (name : string, factory : IResizerFactory) =
      match this.collection.ContainsKey name with
      | false ->
        this.collection.Add (name, factory)
        this
      | _ ->
        invalidOpf "Resizer factory has already been registered for %s" name
    member this.Build () = ResizerCollection this :> IResizerCollection

and
  [<Sealed>]
  private ResizerCollection =
    val private collection : ImmutableDictionary<string, IResizerFactory>

    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    internal new (builder : ResizerCollectionBuilder) =
      let collection = builder.collection.ToImmutableDictionary StringComparer.OrdinalIgnoreCase
      { collection = collection }

    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    member this.TryGetValue name =
      let mutable value = Unchecked.defaultof<_>
      match this.TryGetValue (name, &value) with
      | true -> ValueSome value
      | _    -> ValueNone

    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    member this.TryGetValue (name : string, [<Out>] factory : byref<_>) =
      this.collection.TryGetValue (name, &factory)

    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    member private this.GetEnumerator () = this.collection.GetEnumerator ()

    interface IEnumerable with
      member this.GetEnumerator () = this.GetEnumerator () :> _
    interface IResizerCollection with
      member this.Count   = this.collection.Count
      member this.Keys    = this.collection.Keys
      member this.Values  = this.collection.Values
      member this.Item
        with get name =
          match this.TryGetValue name with
          | ValueSome f -> f
          | _           -> sprintf "No resizer factory registered for %s" name |> KeyNotFoundException |> raise
      member this.ContainsKey name = this.collection.ContainsKey name
      member this.TryGetValue (name, [<Out>] factory : byref<_>) = this.TryGetValue (name, &factory)
      member this.GetEnumerator () = this.GetEnumerator () :> _


type ImageResizerConfigurationBuilder () =

  member val Options = DefaultImageResizerOptions.Instance :> IImageResizerOptions with get, set

  member val ResizerCollectionBuilder =
    ResizerCollectionBuilder()
      .Add("none", NoneResizerFactory ())
      .Add("exact", ExactResizerFactory ())
      .Add("inbox", InboxResizerFactory ())

  member this.ConfigureOptions (configure : Action<ImageResizerOptionsBuilder>) =
    let builder = ImageResizerOptionsBuilder ()
    if not (isNull configure) then
      configure.Invoke builder
    this.Options <- builder.Options
    this

  member this.ClearResizers () =
    this.ResizerCollectionBuilder.collection.Clear ()
    this

  member this.AddResizer (name, factory) =
    this.ResizerCollectionBuilder.Add (name, factory) |> ignore
    this

  member this.AddResizer<'TFactory when 'TFactory :> IResizerFactory and 'TFactory : (new : unit -> 'TFactory)> name =
    this.AddResizer (name, new 'TFactory ())