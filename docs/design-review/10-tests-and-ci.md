# 10. No real tests, no CI, no quality gates — Critical

## Problem

- **The "integration" tests called the live WooliesX API** (`ProductSortController`, `TrolleyTotalTests`, `UserControllerTests`). They break whenever the third-party service is down, slow or retired, which makes them flaky by design.
- **Tests asserted the hard-coded token**, cementing the secret into the test suite.
- **Edge cases were untested.** No tests covered invalid sort options, duplicate names, missing catalog items, upstream failures, auth, or the trolley endpoint beyond the happy path.
- `new WebApplicationFactory<Startup>()` was never disposed.
- **No CI pipeline, no nullable reference types, warnings not treated as errors, no vulnerability scanning.**

## Fix

### Test pyramid — 56 tests, ~1 s, zero network calls

| Layer | What it covers | File |
|---|---|---|
| Domain unit | Sort-option parsing (case, `"1"`, `"Low,High"`); ranking edge cases (duplicates, discontinued, ties, units vs. orders) | [SortOptionsTests.cs](../../ProductCatalog.Tests/SortOptionsTests.cs), [PopularityRankingTests.cs](../../ProductCatalog.Tests/PopularityRankingTests.cs) |
| Application unit | Every sort option; history not fetched unless needed; unknown enum throws | [GetSortedProductsQueryHandlerTests.cs](../../ProductCatalog.Tests/GetSortedProductsQueryHandlerTests.cs) |
| Infrastructure unit | Upstream JSON shape (numeric `customerId`, `0.0` quantities); non-2xx and bad JSON → `UpstreamServiceException`; token appended once and URL-escaped; cache hit, failure not cached, zero TTL bypasses | [WooliesXClientTests.cs](../../ProductCatalog.Tests/WooliesXClientTests.cs), [CachingClientTests.cs](../../ProductCatalog.Tests/CachingClientTests.cs) |
| In-memory integration | Full pipeline via `WebApplicationFactory` with stubbed ports: routes (incl. legacy `/sort`), 400/401/405/429/502, ProblemDetails, no token in error bodies, auth, rate limit | [Integration/](../../ProductCatalog.Tests/Integration/) |
| Composition | The real DI graph resolves; app fails to start without a token | [CompositionTests.cs](../../ProductCatalog.Tests/Integration/CompositionTests.cs) |

[ApiFactory.cs](../../ProductCatalog.Tests/Integration/ApiFactory.cs) overrides configuration in memory and replaces the three upstream ports with stubs. It runs everything else for real: routing, auth, rate limiting, validation, exception handling.

### Quality gates

- [Directory.Build.props](../../Directory.Build.props): `Nullable` enabled, `TreatWarningsAsErrors`, `EnforceCodeStyleInBuild`, latest analyzers. A nullable warning or analyzer hit fails the build.
- [.github/workflows/ci.yml](../../.github/workflows/ci.yml): on every push and PR it restores, builds in Release (warnings are errors), scans packages for vulnerabilities, runs tests with coverage, and uploads the coverage report.
- The vulnerability gate earned its place immediately. It caught that the test project pulled a vulnerable `System.Text.Json 8.0.4` transitively (GHSA-8g4q-xg66-9fp4), now pinned to 8.0.5.
- `.gitignore` added for `bin/`, `obj/` and `TestResults/`.

## What I'd add next

- **Contract tests** against the upstream (Pact, or a scheduled smoke test in a non-blocking pipeline) so we learn when WooliesX changes shape, *without* making PR builds depend on it.
- A coverage threshold, `dotnet format --verify-no-changes` with an `.editorconfig`, mutation testing (Stryker) on the domain, and an architecture test for the layer rules.
- A deployment stage: container build, image scan, deploy to staging, smoke test `/health`.

## What to say in the interview

**30-second answer:**
> "The existing integration tests called the real third-party API, so they were flaky by design, and they asserted the secret. I built a proper pyramid: pure unit tests for the domain and use case, focused tests for the HTTP adapters with a fake handler, and in-memory integration tests that run the real pipeline (routing, auth, rate limiting, error handling) with stubbed upstreams. 56 tests, about a second, no network. Then I added CI with warnings-as-errors, nullable checks and a vulnerability scan, which found a real high-severity transitive vulnerability on its first run."

**Likely follow-ups:**
- *"Mocks or stubs?"* I used hand-written stubs. They're simple, readable, and the interfaces are tiny. A mocking library pays off when interfaces are large or you need interaction verification.
- *"How do you still know the real upstream works?"* With contract tests and a scheduled smoke test that alerts but doesn't block merges. External availability shouldn't gate your PRs.
- *"What's the value of `WebApplicationFactory` over unit-testing controllers?"* It catches what unit tests can't: routing attributes, middleware order (auth before rate limiting), model binding, filters and serialisation. The `/products/sort` vs `/sort` route bug is exactly this kind.
