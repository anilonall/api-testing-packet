# Environments and variables

An environment is a named list of enabled variables. Values can be marked secret in the model; the UI should keep them masked except during deliberate editing.

Resolution precedence, from lowest to highest, is packet defaults, selected environment, then runtime/extracted values. Name matching is case-insensitive. Missing placeholders stop the request and list every missing name.

Extraction sources are response JSON paths and response headers. Scenario runtime values live only for that run and are passed to subsequent steps; they are not written to history snapshots.
