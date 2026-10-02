# Execution engine

`ApiTestingPacket.Core.RequestExecutionEngine` is the single request pipeline used by the API, scenario runner, and CLI:

1. Merge environment and runtime variables.
2. Interpolate URL, parameters, headers, authentication, and body; fail on missing values.
3. validate the resolved URI and DNS answers through the outbound policy.
4. Build the HTTP message and apply authentication.
5. Send with a bounded timeout and redirects disabled.
6. stream the response into a bounded buffer.
7. Evaluate backend assertions and extract JSON/header values.
8. Return a shared result model; the API persists a redacted snapshot.

Cancellation is propagated. DNS, connection, TLS, timeout, malformed URL, response-limit, and variable errors become execution results rather than process crashes.
