module WebhookTest.App
open System
open System.Net
open System.IO

open CoreTweet

open Suave
open Suave.Operators
open Suave.Authentication
open Suave.State.CookieStateStore
open Suave.Cookie

open Newtonsoft.Json

let private serialize m = JsonConvert.SerializeObject m
let private deserialize<'a> json = JsonConvert.DeserializeObject<'a> json

[<Literal>]
let sessionType = "session_type"

type Session with
  member this.set (state: State.StateStore) =
    let sty =
      match this with
        | NoSession          -> SessionType.NoSession
        | AuthorizeSession _ -> SessionType.AuthorizeSession
        | TokensSession _    -> SessionType.TokensSession
        | WebhookSession _   -> SessionType.WebhookSession
    let ret = succeed >=> state.set sessionType sty
    match this with
      | TokensSession tokens ->
        ret >=> state.set "a_token" tokens.AccessToken
            >=> state.set "a_secret" tokens.AccessTokenSecret
      | WebhookSession (tokens, webhook) ->
        ret >=> state.set "a_token" tokens.AccessToken
            >=> state.set "a_secret" tokens.AccessTokenSecret
            >=> state.set "webhook" (serialize webhook)
      | AuthorizeSession auth ->
        ret >=> state.set "r_token" auth.RequestToken
            >=> state.set "r_secret" auth.RequestTokenSecret
      | NoSession -> ret

  static member parse (state: State.StateStore) =
    match state.get sessionType with
      | None
      | Some SessionType.NoSession -> NoSession
      | Some SessionType.AuthorizeSession ->
        let os = OAuth.OAuthSession()
        os.ConsumerKey <- Property.consumerKey
        os.ConsumerSecret <- Property.consumerKey
        os.RequestToken <- state.get "r_token" |> Option.get
        os.RequestTokenSecret <- state.get "r_secret" |> Option.get
        AuthorizeSession os
      | Some x ->
        let tokens = 
          Tokens.Create (
            Property.consumerKey,
            Property.consumerSecret,
            state.get "a_token" |> Option.get,
            state.get "a_secret" |> Option.get
          )
        match x with
          | SessionType.TokensSession -> TokensSession tokens
          | SessionType.WebhookSession ->
            WebhookSession (tokens, state.get "webhook" |> Option.get |> deserialize)
          | _ -> undefined ()

let sessionSet (session: Session) =
  statefulForSession >=> context (fun x ->
    match HttpContext.state x with
      | Some state -> 
        session.set state
      | None -> never
  )

let private sessionGet def f =
  statefulForSession >=> context (fun x ->
    match HttpContext.state x with
      | Some state ->
        f state
      | None ->
        def
  )

let session f =
  sessionGet (f NoSession) (Session.parse >> f)

let clearSession =
  unsetPair SessionAuthCookie
  >=> unsetPair StateCookie

let returnToHome =
  Redirection.redirect "/"

let returnPathOrHome = 
  request <|
    fun x -> 
      let path = 
        match (x.queryParam "returnPath") with
          | Choice1Of2 path -> path
          | _ -> "/"
      Redirection.FOUND path
