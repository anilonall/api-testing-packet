# Security model

## Accounts and isolation

Passwords use BCrypt cost 12. JWT access tokens expire after 15 minutes by default. Refresh tokens are 384-bit random values delivered in HttpOnly, SameSite cookies, stored server-side only as SHA-256 hashes, rotated on use, and revocable. Every workspace query includes the authenticated user ID; knowledge of another UUID does not grant access. Authentication endpoints are IP rate-limited.

## Outbound requests

Only HTTP(S) is accepted. In production, DNS answers resolving to loopback, private IPv4, link-local, unspecified, or IPv6 site-local addresses are rejected. Redirects and cookies are disabled, time and response size are bounded, and production private-network access requires explicit configuration. Network egress firewall rules remain recommended because application checks cannot fully eliminate rebinding and topology-specific SSRF risk.

## Secrets

Authorization, cookie, API-key, token/password query values, extracted runtime variables, and marked environment secrets must not enter logs, exports, snapshots, or reports. Database volume encryption and operational backup security are deployment responsibilities. V1 does not provide application-level encryption at rest.
