namespace NCoreUtils.Images

open System
open Newtonsoft.Json
open NCoreUtils
open System.Collections.Generic
open System.Reflection

[<Struct>]
type internal BufferedJsonToken = {
  ValueType : Type
  Value     : obj
  Path      : string
  Depth     : int
  TokenType : JsonToken }

module internal BufferedJsonToken =

  let peek (reader : JsonReader) =
    { ValueType = reader.ValueType
      Value     = reader.Value
      Path      = reader.Path
      Depth     = reader.Depth
      TokenType = reader.TokenType }

type internal BufferedJsonReader (tokens : IReadOnlyList<BufferedJsonToken>) =
  inherit JsonReader ()
  do if isNull tokens then ArgumentNullException "tokens" |> raise
  let mutable offset = 0
  member __.CurrentToken with inline get () = tokens.[offset]
  override this.ValueType = this.CurrentToken.ValueType
  override this.Value     = this.CurrentToken.Value
  override this.Path      = this.CurrentToken.Path
  override this.Depth     = this.CurrentToken.Depth
  override this.TokenType = this.CurrentToken.TokenType
  override __.Read () =
    offset <- offset + 1
    offset < tokens.Count

[<AutoOpen>]
module private ImageResizerErrorConverterHelpers =
  let name2type =
    Map.ofList
      [ "generic",             typeof<ImageResizerError>
        "invalid_resize_mode", typeof<InvalidResizeModeError>
        "invalid_image_type",  typeof<InvalidImageTypeError> ]
  let type2name =
    name2type |> Seq.map (fun kv -> kv.Value, kv.Key) |> Seq.toArray

  let inline readOrFail (reader : JsonReader) =
    if not <| reader.Read () then
      invalidOp "Unexpected end of json stream"

  let inline ensure expected (reader : JsonReader) =
    if reader.TokenType <> expected then
      invalidOpf "Expected %A, got %A." expected reader.TokenType

  let inline readExactOrFail expected reader =
    readOrFail reader
    ensure expected reader

type ImageResizerErrorConverter () =
  inherit JsonConverter ()
  abstract NameToType : name:string -> Type
  abstract TypeToName : ``type``:Type -> string
  default __.NameToType name =
    match Map.tryFind name name2type with
    | Some ``type`` -> ``type``
    | _             -> invalidOpf "No image resizer error registered for name %s" name
  default __.TypeToName ``type`` =
    match Array.tryFind (fst >> ((=) ``type``)) type2name >>| snd with
    | Some name -> name
    | _             -> invalidOpf "Not registered image resizer error of type %s" ``type``.FullName
  override __.CanConvert objectType = Seq.exists (fst >> ((=) objectType)) type2name
  override this.ReadJson (reader, _, _, serializer) =
    let preload = Dictionary ()
    ensure JsonToken.StartObject reader
    let mutable dynamicType = null
    readOrFail reader
    while isNull dynamicType do
      ensure JsonToken.PropertyName reader
      let propertyName = reader.Value :?> string
      match propertyName with
      | "type" ->
        readExactOrFail JsonToken.String reader
        let name = reader.Value :?> string
        dynamicType <- this.NameToType name
        readOrFail reader
      | _ ->
        // preload property data
        let depth = reader.Depth
        let buffer = ResizeArray ()
        readOrFail reader
        while (reader.TokenType <> JsonToken.PropertyName && reader.TokenType <> JsonToken.EndObject && reader.Depth <> depth) || reader.Depth > depth do
          BufferedJsonToken.peek reader |> buffer.Add
          readOrFail reader
        preload.Add (propertyName, buffer)
    // type found, proceed with preloaded properties
    let obj = Activator.CreateInstance dynamicType
    for kv in preload do
      let propertyName = kv.Key
      let property = dynamicType.GetProperty (propertyName, BindingFlags.Public ||| BindingFlags.Instance ||| BindingFlags.FlattenHierarchy ||| BindingFlags.IgnoreCase)
      use bufferedReader = new BufferedJsonReader (kv.Value)
      let propObj = serializer.Deserialize (bufferedReader, property.PropertyType)
      property.SetValue (obj, propObj, null)
    // read other properties
    while JsonToken.EndObject <> reader.TokenType do
      ensure JsonToken.PropertyName reader
      let propertyName = reader.Value :?> string;
      let property = dynamicType.GetProperty (propertyName, BindingFlags.Public ||| BindingFlags.Instance ||| BindingFlags.FlattenHierarchy ||| BindingFlags.IgnoreCase)
      readOrFail reader
      let propObj = serializer.Deserialize (reader, property.PropertyType)
      property.SetValue (obj, propObj, null)
      readOrFail reader
    obj

  override this.WriteJson (writer, value, serializer) =
    match isNull value with
    | true -> writer.WriteNull ()
    | _ ->
      let name = this.TypeToName <| value.GetType ()
      writer.WriteStartObject ()
      writer.WritePropertyName "type"
      writer.WriteValue name
      let properties = value.GetType().GetProperties (BindingFlags.Public ||| BindingFlags.Instance ||| BindingFlags.FlattenHierarchy);
      for property in properties do
        let name = property.Name.Substring(0, 1).ToLowerInvariant() + property.Name.Substring(1)
        writer.WritePropertyName name
        serializer.Serialize (writer, property.GetValue (value, null))
        writer.WriteEndObject ()