**WORK IN PROGRESS**

**FOR TESTING/REFERENCE - DO NOT EXPECT PRODUCTION QUALITY**

This is a web app for testing the Twitter Account Activity API.

## How to run

1. create `.env` with the following content:

```
CK=<your Twitter app's consumer key>
CS=<your Twitter app's consumer secret>
```

2. `dotnet build`

3. `dotnet run -p src/WebhookTest.fsproj`

## Command options

```
  --port, -p=<ip>                   set the port to listen (default: 8080)
  --ip, -i=<b>.<b>.<b>.<b>          set a IP address to bind the server (default: 127.0.0.1)
  --webhook-host, -w=<host>         set a hostname for receiving webhook, including http[s]:// (default: same as in --host)
  --host, -h=<host>                 set a hostname of this server, including http[s]:// (default: http://127.0.0.1:8080)
```

