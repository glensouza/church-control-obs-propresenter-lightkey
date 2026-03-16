Repository: church-control-obs-propresenter-lightkey

Inferred coding style and editor preferences (extracted from recent local changes):

- C# / project
  - Target: .NET 10, C# 14
  - Project type: Blazor (components under `Components/Pages`)

- Naming
  - Private fields: use `camelCase` without a leading underscore (e.g., `obsHost`, `obsPort`).
  - Acronyms: avoid all-caps identifiers; use PascalCase/capitalized acronym form (e.g., `ObsService` not `OBSService`).
  - Public properties and services: use `PascalCase` (e.g., `ObsService`).
  - Async methods keep the `Async` suffix.

- Code style
  - Prefer explicit typed `out` variables (e.g., `int.TryParse(..., out int p)` instead of `out var p`).
  - Keep private state as simple primitive fields when appropriate (e.g., `private int obsPort = 4455;`).

- Other notes
  - Follow existing project conventions for logging and null checks.
  - Do not modify language or target frameworks without explicit consent.

How to use
- This file is a living guide for Copilot/AI assistants. When suggesting code, prefer the rules above.
- If you want these rules changed, edit this file and commit the update.

Generated: by an automated workspace inspection tool.
