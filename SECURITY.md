# Security Policy

## Supported versions

Security fixes are applied to the latest commit on `main`. This portfolio project does not maintain older release branches.

## Reporting a vulnerability

Use GitHub private vulnerability reporting for this repository. Include:

- the affected route or component;
- reproducible steps;
- expected and actual behavior;
- impact and any suggested mitigation.

Do not open a public issue containing exploit details, credentials, or personal data.

## Deployment warning

IT Equipment Checkout intentionally has no authentication or authorization. It must not be exposed directly to the public internet. Deploy it only on a trusted local network or behind an authenticated reverse proxy with TLS.

Seed data is fictional. Do not submit real employee or equipment records in bug reports or test fixtures.

## Dependency policy

Dependencies and container bases are pinned and reviewed through Dependabot. CI audits NuGet packages and scans the final image with Trivy. A clean scan reduces known risk but is not a security guarantee.
