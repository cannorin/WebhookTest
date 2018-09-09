[<AutoOpen>]
module WebhookTest.BasicTypes
open CoreTweet
open CoreTweet.AccountActivity
open Suave
open Suave.Operators

type Session = 
  | NoSession
  | AuthorizeSession of OAuth.OAuthSession
  | TokensSession of Tokens
  | WebhookSession of Tokens * Webhook

type SessionType = 
  | NoSession = 0
  | AuthorizeSession = 1
  | TokensSession = 2
  | WebhookSession = 3

let (|LoggedInSession|GuestSession|) = function
  | TokensSession t | WebhookSession (t, _) -> LoggedInSession t
  | _ -> GuestSession
