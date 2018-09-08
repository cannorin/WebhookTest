[<AutoOpen>]
module WebhookTest.BasicTypes
open CoreTweet

type Session = 
  | NoSession
  | AuthorizeSession of OAuth.OAuthSession
  | TokensSession of Tokens

 