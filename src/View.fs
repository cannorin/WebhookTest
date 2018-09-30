module WebhookTest.View

open System
open CoreTweet
open Suave.Html

let inline h i attr contents =
  tag (sprintf "h%i" i) attr contents

let mainPage session info =
  html [] [
    head [] [
      title [] "WebhookTest"
    ]
    body [] [
      yield h 1 ["id", "session-info"] <| text "Session Info"
      yield p [] <| text info
      yield h 1 ["id", "menu"] <| text "Menu"
      yield! List.map (List.singleton >> p []) <| 
        match session with
          | TokensSession token -> 
            [
              a "/profile" [] <| text "Profile"
              a "/logout" []  <| text "Logout"
              a "/webhook_test" [] <| text "Activate Webhook"
            ]
          | WebhookSession (token, webhook) -> 
            [
              a "/profile" [] <| text "Profile"
              a "/webhook_test" [] <| text "Deactivate Webhook"
            ]
          | NoSession
          | AuthorizeSession _ -> [a "/twitter_login" [] <| text "Login"]
    ]
  ] |> htmlToString

let profilePage session =
  html [] [
    head [] [
      title [] "WebhookTest - User Profile"
    ]
    body []  <|
      match session with
        | LoggedInSession token -> 
          let user = token.Account.VerifyCredentials()
          [
            p [] <| text (sprintf "%s (@%s)" user.Name user.ScreenName)
            img ["src", user.ProfileImageUrlHttps]
            p [] <| text user.Description
            a user.Url [] <| text user.Url
            p [] <| text (sprintf "%i follows / %i followers" user.FriendsCount user.FollowersCount)
            hr []
            a "/" [] <| text "Home"
          ]
        | GuestSession ->
          [
            a "/twitter_login" [] <| text "Please login"
          ]
  ] |> htmlToString

