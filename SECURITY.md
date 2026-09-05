# Security Policy

We take security very seriously, continuously review security vulnerabilities within the code,
and try to provide fixes in a timely manner.

S2 Connect is a security protocol: pairing tokens, HMAC challenge responses, access tokens and
communication tokens are credentials. Please read the "Security (normative)" section of the
S2 Connect specification (https://docs.s2standard.org/s2-connect/1.0.0/) and the redaction
rules in `PLAN.md` §3.6 before reporting or fixing an issue in these areas.

## Known limitations

* **Secrets are stored in clear text by default.** `JSONFileS2Store` persists the access token of
  every pairing and every pending access token, and its `ISecretProtector` hook defaults to a
  no-op. Anyone who can read that file can impersonate a paired node until the token is rotated,
  so protect it with file permissions and, where it matters, an encrypted file system - or plug in
  your own protector. See `PLAN.md` §11 for the open design question behind this.

## Reporting Security Vulnerabilities

Please report security vulnerabilities in the Issues section of this repository. By keeping
security issues visible, we leverage our community in helping fix them promptly.

If you are concerned about sharing a vulnerability with the rest of the community, you can also
send your report to: github@graphdefined.com
