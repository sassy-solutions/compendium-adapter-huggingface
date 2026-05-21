# Changelog

All notable changes to `Compendium.Adapters.HuggingFace` are documented here.
The project follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- Initial implementation of `Compendium.Adapters.HuggingFace`, a direct adapter against Hugging Face inference surfaces for [`Compendium.Abstractions.AI`](https://www.nuget.org/packages/Compendium.Abstractions.AI) 1.0.1.
- Two-mode design via `HuggingFaceMode`:
  - `InferenceEndpoint` (default) — dedicated per-tenant URL under `*.endpoints.huggingface.cloud`, recommended for production.
  - `ServerlessInference` — shared API at `https://api-inference.huggingface.co/models/<id>`, free tier + Pro; prototyping only.
- `HuggingFaceAIProvider` implementing `IAIProvider`:
  - Chat completions via the OpenAI-compatible Messages API (`/v1/chat/completions`) for both modes.
  - Streaming completions via SSE for both modes.
  - Embeddings via the *feature-extraction* task, with auto-detection of single-vector / batch / token-matrix response shapes (token matrices are mean-pooled to a single vector per input).
  - Best-effort tool / function calling pass-through (works on TGI-backed endpoints; silently degrades on non-TGI backends).
  - Health check via a cheap one-input embedding probe.
- `HuggingFaceHttpClient` typed `HttpClient` with Bearer auth, per-mode URL resolution, SSE stream reader, and dual error-body shape parsing (`{"error":"string"}` vs `{"error":{"message":"…"}}`).
- `Microsoft.Extensions.Http.Resilience` standard pipeline wired via `AddCompendiumHuggingFace`.
- Error mapping for HTTP 401 / 402 / 404 / 429 / 5xx into `AIErrors.*` codes; caller-cancellation rethrown, other `TaskCanceledException`s mapped to `AIErrors.Timeout`.
- `HuggingFaceOptions` with sensible defaults (Llama-3.1-8B-Instruct chat, all-MiniLM-L6-v2 embeddings, 120s timeout, mode-aware validation).
- Sample [`samples/01-multi-mode`](samples/01-multi-mode) — single program demonstrating both modes side-by-side.

### Notes

- Unit test suite: 102 tests, 98.4 % line coverage, 94.3 % branch coverage on the unit-testable surface.
- No NuGet release tagged yet — orchestrator tags after merge to `main`.
