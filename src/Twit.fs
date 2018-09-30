module WebhookTest.Twit
open System
open System.Text
open System.Security.Cryptography
open CoreTweet
open CoreTweet.AccountActivity

open Suave
open Suave.Operators
open Suave.Authentication
open System.Diagnostics

open Newtonsoft.Json
open Newtonsoft.Json.Linq

let inline getEnv (token: CoreTweet.Core.TokensBase) =
  token.AccountActivity.Premium.Env("CoreTweetTest")

let appEnv = getEnv oauth2Token

let login srv =
  authenticated Cookie.CookieLife.Session false >=> App.session (
    fun session ->
      match session with
        | AuthorizeSession os ->
          App.clearSession
          >=> Redirection.redirect "/twitter_login"
        | TokensSession _ | WebhookSession _ ->
          App.returnToHome
        | NoSession ->
          let oas =
            OAuth.Authorize (
              consumerKey,
              consumerSecret,
              oauthCallback=(srv |> Server.path ["twitter_login_redirect"])
            )
          App.sessionSet (AuthorizeSession oas) 
          >=> Redirection.redirect (oas.AuthorizeUri.ToString()) 
    )

let redirect =
  App.session (fun session ->
    match session with
      | AuthorizeSession os ->
          request <|
            fun r ->
              match r.queryParam "oauth_verifier" with
                | Choice1Of2 pin -> 
                  let tokens = os.GetTokens(pin) in
                  App.sessionSet (TokensSession tokens)
                  >=> App.returnToHome
                | Choice2Of2 msg -> 
                  App.clearSession
                  >=> App.returnToHome
      | _ -> 
        App.returnToHome
  )

let webhookManage srv =
  App.session <|
    function
      | TokensSession tokens ->
        let env = getEnv tokens

        for w in appEnv.GetWebhooks() do
          printfn "* Deactivating an existing webhook '%s'" w.Id
          env.DeleteWebhooks w.Id

        let webhook_url = srv |> Server.webhookPath
        printfn "* Registering a new webhook URL '%s'" webhook_url
        let webhook = env.PostWebhooks webhook_url

        printfn "* Subscribing the app to Account Activity events"
        env.PostSubscriptions()

        App.sessionSet (WebhookSession(tokens, webhook)) >=> App.returnToHome
      | WebhookSession (tokens, webhook) ->
        let env = getEnv tokens

        printfn "* Deactivating all subscriptions"
        env.DeleteSubscriptions()

        printfn "* Deactivating a webhook '%s'" webhook.Id
        env.DeleteWebhooks webhook.Id
        
        App.sessionSet (TokensSession tokens) >=> App.returnToHome
      | _ -> App.returnToHome  

let webhookChallenge =
  request <|
    fun r ->
      match r.queryParam "crc_token" with
        | Choice1Of2 crcToken ->
          let json = Property.oauth2Token.GenerateCrcJsonResponse(crcToken)
          printfn "* Incoming CRC, responding"
          Successful.OK json
        | Choice2Of2 _ ->
          RequestErrors.BAD_REQUEST "no crc_token"

let webhookReceive =
  let validate (r: HttpRequest) (payload: string) =
    match r.header "x-twitter-webhooks-signature" with
      | Choice1Of2 sgn ->
        use hmacsha256 = new HMACSHA256(Encoding.UTF8.GetBytes Property.consumerSecret)
        let correct = 
          hmacsha256.ComputeHash(Encoding.UTF8.GetBytes payload) 
          |> Convert.ToBase64String
          |> sprintf "sha256=%s"
        if correct <> sgn then
          printfn "[?] payload validation failed:"
          printfn " -  received: %s" sgn
          printfn " -  computed: %s" correct
          false
        else
          true
      | _ ->
        printfn "[?] payload validation failed (no signature)"
        false
  request <|
    fun r ->
      let payload = r.rawForm |> Encoding.UTF8.GetString
      if validate r payload then
        try
          let msg = ActivityEvent.Parse payload
          ObjectDumper.Dump(msg, DumpStyle.Console)
          |> printfn "* Incoming webhook:\n%s\n" 
        with
          | :? ParsingException as e ->
            printfn "[!] parsing exception: %A" e
            printfn " -  extracted payload: \n%s"
                    (JObject.Parse(e.Json).ToString(Formatting.Indented))
          | e ->
            printfn "[!] unknown error: %A" e
        Successful.OK "OK"
      else
        RequestErrors.UNAUTHORIZED "payload cannot be verified"
