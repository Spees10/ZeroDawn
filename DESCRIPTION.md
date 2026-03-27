# ZeroDawn Starter Template

ZeroDawn is a production-oriented starter template for building new business apps faster across:

- ASP.NET Core Web API
- Blazor Web client
- .NET MAUI Windows / Android app

It gives you a ready foundation instead of starting from scratch every time. The template already includes:

- Authentication and authorization structure
- Role-based dashboards and admin pages
- Shared Blazor UI components and layouts
- Localization and RTL support
- Logging, error handling, health checks, and rate limiting
- Email flow structure
- Typed API clients and shared contracts
- MAUI storage/connectivity abstractions
- Documentation for running, publishing, and handoff

This repository is meant to be copied and used as the starting point for a new project idea, then re-branded and re-configured for that specific product.

## Important

After cloning or copying this template, read this file first before changing anything:

- [ReadMeFirst.md]

That file contains the bootstrap prompt and the required project-specific values you must replace at the beginning of every new idea, such as:

- Project name and branding
- Ports and base URLs
- Connection string
- JWT secret
- SMTP settings
- Seeded admin account
- App identifiers and manifest values
- Documentation and localization values

## Intended Use

Use this template when you want:

- A reusable app foundation
- Shared UI between Web and MAUI
- Faster setup for auth, admin, profile, and system pages
- A cleaner handoff to AI-assisted development

Do not treat it as a finished generic product. It is a strong base, but every new project must go through the bootstrap/replacement step in `ReadMeFirst.md`.
