module internal NCoreUtils.Images.WebService.Config

open System
open System.Net.Http
open System.Text.RegularExpressions
open Microsoft.Extensions.Configuration
open NCoreUtils

exception InitializationException of Msg:string
  with
    override this.Message =
      match this :> exn with
      | InitializationException msg -> msg
      | _                           -> Unchecked.defaultof<_>

type private Var = {
  VarName             : string
  ConfigPath          : string
  DefaultValue        : string option
  DefaultValueFactory : (unit -> string option) option
  IsSensitive         : bool }

let private v name path def f isSensitive =
  { VarName             = name
    ConfigPath          = path
    DefaultValue        = def
    DefaultValueFactory = f
    IsSensitive         = isSensitive }

let private vx name path def f = v name path def f false

let private vy name path def f = v name path def f true

let private getLocation =
  let regex = Regex "(europe|us|asia)-[a-z]+[0-9]"
  fun (str : string) -> regex.Match(str).Groups.[0].Value

let private getGoogleMetadata (uri : string) =
  try
    use client = new HttpClient ()
    use request = new HttpRequestMessage (HttpMethod.Get, uri)
    request.Headers.Add ("Metadata-Flavor", "Google")
    use response = client.SendAsync(request).Result
    response.Content.ReadAsStringAsync().Result |> Some
  with exn ->
    eprintfn "Unable to get google metadata (uri = %s): %s" uri exn.Message
    None

let private vars =
  [ vx "GOOGLE_PROJECTID"   "Google:ProjectId"         None         (Some (fun () -> getGoogleMetadata "http://metadata.google.internal/computeMetadata/v1/project/project-id"))
    vx "GOOGLE_SERVICENAME" "Google:ServiceName"       None          None
    vx "QUALITY_DEFAULT"    "Images:Quality:Default"  (Some "85")    None
    vx "OPTIMIZE_DEFAULT"   "Images:Optimize:Default" (Some "false") None ]

let buildConfigFromEnv () =
  let map =
    vars
    |> Seq.fold
      (fun map var ->
        match Environment.GetEnvironmentVariable var.VarName with
        | null ->
          match var.DefaultValue with
          | Some v ->
            match var.IsSensitive with
            | true -> printfn "%s not defined in environment, using default value." var.VarName
            | _    -> printfn "%s not defined in environment, using default value => \"%s\"." var.VarName v
            Map.add var.ConfigPath v map
          | _ ->
          match var.DefaultValueFactory >>= (fun f -> f ()) with
          | Some v ->
            match var.IsSensitive with
            | true -> printfn "%s not defined in environment, using generated value." var.VarName
            | _    -> printfn "%s not defined in environment, using generated value => \"%s\"." var.VarName v
            Map.add var.ConfigPath v map
          | _ ->
            sprintf "Unable to get value for %s." var.VarName
            |> InitializationException
            |> raise
        | v ->
          match var.IsSensitive with
          | true -> printfn "Using environment value for %s." var.VarName
          | _    -> printfn "Using environment value for %s. => \"%s\"." var.VarName v
          Map.add var.ConfigPath v map
      )
      Map.empty
  ConfigurationBuilder().AddInMemoryCollection(map).Build ()

let buildConfigFromJson () =
  ConfigurationBuilder()
    .SetBasePath(System.IO.Directory.GetCurrentDirectory ())
    .AddJsonFile("appsettings.json", reloadOnChange = false, optional = true)
    .Build ()

let buildConfig () =
  match System.Environment.GetEnvironmentVariable "ASPNETCORE_CONFIG" with
  | EQI "env" -> buildConfigFromEnv  ()
  | _         -> buildConfigFromJson ()