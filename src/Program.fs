module WebhookTest.Main

open System
open System.Net
open CoreTweet
open CoreTweet.AccountActivity

open FSharp.CommandLine.Options
open FSharp.CommandLine.OptionValues
open FSharp.CommandLine.Commands

open Suave
open Suave.Filters
open Suave.Operators

open WebhookTest

let config srv = 
  { 
    defaultConfig with
      bindings = [ HttpBinding.create HTTP srv.bindingIP srv.listeningPort ] 
  }

let webPart srv =
  choose [
    path "/twitter_login" >=>
      Twit.login srv

    path "/twitter_login_redirect" >=>
      Twit.redirect

    path "/logout" >=>
      App.clearSession >=> App.returnToHome

    path "/profile" >=>
      App.session (fun session ->
        View.profilePage session |> Successful.OK
      )

    path "/webhook_test" >=>
      Twit.webhookManage srv

    path "/twitter_webhook" >=>
      choose [
        GET  >=> Twit.webhookChallenge
        POST >=> Twit.webhookReceive
      ]

    path "/" >=>
      App.session (fun session ->
        let state =
          match session with
            | AuthorizeSession os ->
              sprintf "AuthorizeSession: %A" os.AuthorizeUri
            | TokensSession t ->
              t.Account.VerifyCredentials().ScreenName
                |> sprintf "TokensSession: %s"
            | WebhookSession (t, w) ->
              let name = t.Account.VerifyCredentials().ScreenName
              sprintf "WebhookSession: %s, id: %s, since: %A" name w.Id w.CreatedAt
            | _ -> 
              "None"
        View.mainPage session state |> Successful.OK
      )
  ]

let hostnameOpt =
  commandOption {
    names ["host"; "h"]
    description "set a hostname of this server, including http[s]:// (default: http://127.0.0.1:8080)"
    takes (format("%s").withNames["host"])
  } |> CommandOption.zeroOrExactlyOne

let webhookHostOpt =
  commandOption {
    names ["webhook-host"; "w"]
    description "set a hostname for receiving webhook, including http[s]:// (default: same as in --host)"
    takes (format("%s").withNames["host"])
  } |> CommandOption.zeroOrExactlyOne

let bindingIPOpt =
  commandOption {
    names ["ip"; "i"]
    description "set a IP address to bind the server (default: 127.0.0.1)"
    takes (format("%i.%i.%i.%i").withNames["b";"b";"b";"b"])
  } |> CommandOption.zeroOrExactlyOne
    |> CommandOption.map (function
         | Some (a,b,c,d) -> new IPAddress([|uint8 a; uint8 b; uint8 c; uint8 d|]) |> Some
         | None -> None
       )

let listeningPortOpt =
  commandOption {
    names ["port"; "p"]
    description "set the port to listen (default: 8080)"
    takes (format("%i").map(fun ip -> uint16 ip))
  } |> CommandOption.zeroOrExactlyOne

let mainCmd =
  command {
    name "WebhookTest"
    description "Sample server app using Twitter Account Activity API"
    opt host in hostnameOpt
    opt wh   in webhookHostOpt
    opt ip   in bindingIPOpt
    opt port in listeningPortOpt
    let srv = {
      Server.defaultSetting with
        hostname      = host ?| Server.defaultSetting.hostname
        webhookHost   = wh
        bindingIP     = ip   ?| Server.defaultSetting.bindingIP
        listeningPort = port ?| Server.defaultSetting.listeningPort
    }
    printfn "* Starting server..."
    startWebServer (config srv) (webPart srv)
    printfn "* Exiting server..."
    return 0
  }

[<EntryPoint>]
let main argv =
  mainCmd |> Command.runAsEntryPoint argv
