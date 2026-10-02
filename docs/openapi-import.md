# OpenAPI import

The API endpoint `/api/import/openapi` accepts an OpenAPI 3 document as JSON or YAML, an explicit format, and an optional collection name. Parsing and validation complete before EF begins persisting the collection.

V1 imports the first server URL (or `{{baseUrl}}`), path operations for the seven supported HTTP methods, query/header parameters, and JSON body examples. Parameters start disabled so incomplete examples are not sent accidentally.

Callbacks, links, cookie parameters, complex schema example synthesis, non-JSON bodies, and automatic OAuth configuration are omitted or reported as warnings. Broken documents return validation problem details and do not create partial data.
