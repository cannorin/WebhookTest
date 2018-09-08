module WebhookTest.Property
open System
open System.IO

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

let hostName =
  if isNull <| Environment.GetEnvironmentVariable("PORT") then
    "http://127.0.0.1:8080"
  else
    "https://coretweet-webhook-test-1.herokuapp.com"

let oauthRedirectPath =
  sprintf "%s/twitter_login_redirect" hostName
