# Native packet format

Files use `.atp.json` and must contain `schemaVersion: 1`, a name, and a `requests` array. Optional `environments` and `scenarios` use the same contracts as interactive execution.

Authentication kinds are `None`, `Bearer`, `Basic`, and `ApiKey`. Body kinds are `None`, `Json`, `Text`, `FormUrlEncoded`, and `Multipart`. Enum values are strings. See `examples/httpbin-workflow.atp.json` for a complete document.

Imports validate the version and item limit before adding a collection. Exports do not include environment secrets by default. Reports and history never include extracted values because they commonly contain tokens.
