[<AutoOpen>]
module WebhookTest.Property

open System
open System.IO
open System.Net
open CoreTweet

let consumerKey =
  if File.Exists ".env" then
    File.ReadLines ".env"
      |> Seq.find (String.startsWith "CK=")
      |> String.split '='
      |> Seq.skip 1 |> Seq.head
  else
    Environment.GetEnvironmentVariable("CK")

let consumerSecret =
  if File.Exists ".env" then
    File.ReadLines ".env"
      |> Seq.find (String.startsWith "CS=")
      |> String.split '='
      |> Seq.skip 1 |> Seq.head
  else
    Environment.GetEnvironmentVariable("CS")

let oauth2Token =
  if File.Exists ".env" then
    match File.ReadLines ".env" |> Seq.tryFind (String.startsWith "BEARER=") with
      | Some l ->
        let b = l |> String.split '=' |> Seq.skip 1 |> Seq.head
        OAuth2Token.Create(consumerKey, consumerSecret, b)
      | None ->
        printf "* Getting a new OAuth2 app-only token... "
        let t = OAuth2.GetToken(consumerKey, consumerSecret)
        File.AppendAllLines(".env", [sprintf "BEARER=%s" t.BearerToken])
        printfn "Done, appended to .env"
        t
  else
    let b = Environment.GetEnvironmentVariable("BEARER")
    OAuth2Token.Create(consumerKey, consumerSecret, b)

type Server = {
  hostname: string
  webhookHost: string option
  bindingIP: IPAddress
  listeningPort: uint16
} with
  static member defaultSetting =
    {
      hostname      = "http://127.0.0.1:8080"
      webhookHost   = None
      bindingIP     = IPAddress.Parse("127.0.0.1")
      listeningPort = uint16 8080 
    }
  static member path paths this =
    Path.Combine(this.hostname, paths |> String.concat "/")
  static member webhookPath this =
    Path.Combine(this.webhookHost ?| this.hostname, "twitter_webhook")
