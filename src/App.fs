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

let sessionSet kvs =
  statefulForSession >=> context (fun x ->
    match HttpContext.state x with
      | Some state -> 
        List.fold (fun s (key, value) -> s >=> state.set key value) succeed kvs
      | None -> never
  )

let sessionGet def f =
  statefulForSession >=> context (fun x ->
    match HttpContext.state x with
      | Some state ->
        f state
      | None ->
        def
  )

let session f =
  sessionGet (f NoSession) <| 
    fun state ->
      match state.get "r_token", state.get "r_secret", state.get "a_key", state.get "a_secret" with
        | Some r_token, Some r_secret, None, None -> 
          let os = OAuth.OAuthSession()
          os.ConsumerKey <- Property.consumerKey
          os.ConsumerSecret <- Property.consumerKey
          os.RequestToken <- r_token
          os.RequestTokenSecret <- r_secret
          AuthorizeSession os |> f
        | _, _, Some a_key, Some a_secret ->
          Tokens.Create (Property.consumerKey, Property.consumerSecret, a_key, a_secret)
            |> TokensSession
            |> f
        | _ -> f NoSession

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
