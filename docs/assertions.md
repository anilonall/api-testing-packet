# Assertions

Assertions execute in the shared backend engine. Supported kinds are status, duration, header, JSON value, JSON type, array length, format, required fields, and recursive sensitive fields. Operators include equality/inequality, existence, containment, membership, and numeric comparisons.

JSON paths support dotted properties and zero-based array indexes, such as `data[0].user.id`. Formats include email, UUID, HTTP(S) URL, ISO date, and ISO datetime. Duration expected values accept milliseconds (`500ms`), seconds (`2s`), or a millisecond number.

The sensitive-field check looks recursively for `password`, `passwordHash`, `secret`, and `privateKey`. It is a focused regression guard, not a vulnerability scanner.
