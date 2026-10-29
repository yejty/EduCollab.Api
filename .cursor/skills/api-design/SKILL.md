---
name: api-design
description: Apply REST API design rules for resource-oriented URLs, query parameters, HTTP methods, content negotiation, error contracts, versioning, and request logging. Use when designing, reviewing, implementing, or documenting HTTP APIs, REST endpoints, OpenAPI specs, request/response contracts, query filters, pagination, sorting, ProblemDetails errors, or API versioning.
---

# API Design

Use this skill when designing, reviewing, implementing, or documenting REST-style HTTP APIs after requirements are known. Treat the API specification as the contract and source of truth for implementation and tests.

## Review Workflow

1. Start from the domain model and expose domain entities as resources.
2. Define the specification before implementation details.
3. Check the base URL structure, resource paths, identifiers, resource representation, query model, methods, error model, versioning, and logging as one contract.
4. Prefer boring consistency over clever endpoint-specific conventions.

## DO

**Specification first**
- DO design the API and write/update the specification before implementation.
- DO treat the specification as the binding source of truth for implementation, integration, documentation, mocks, and tests.
- DO use the specification for documentation, mocks, generated clients or server skeletons, and contract tests when the toolchain supports it.

**Resource URLs**
- DO use resource-based URL design derived from the domain model.
- DO separate audience, version, and resource path deliberately when needed, for example `/public/v1/customers`.
- DO expose domain entities as resources with plural nouns: `users`, `customers`, `invoices`.
- DO identify a collection as `resources` and an instance as `resources/{id}`.
- DO keep resource identifiers in the URL path: `customers/{id}`.
- DO make each URL identify one unique data set.
- DO prefer short URLs when the resource is still uniquely identified.
- DO model separately retrieved or updated 1:1 related data as its own pseudoresource collection: `user-profiles/{id}`, `user-images/{id}`.
- DO return `GET resources/{id}` as a single object response, not as a filtered collection.
- DO choose stable external identifiers; avoid mutable values such as email addresses, and document whether IDs are server-generated or client-supplied.
- DO remember that the public API contract does not have to mirror database tables or CRUD boundaries.

**Query design**
- DO use query parameters for filtering, sorting, and pagination.
- DO treat sparse fieldsets and expansion, such as `fields` and `expand`, as deliberate contract features; prefer granular resources unless there is a clear client need.
- DO filter related collections through the resource collection query: `invoices?orderId={orderId}&customerId={customerId}`.
- DO define defaults for list endpoints, such as first page, default page size, non-deleted records, and deterministic sorting.
- DO validate query inputs, including allowed sort fields, maximum page size, filter formats, and invalid combinations.
- DO reject invalid query input instead of silently clamping or changing behavior, for example `pageSize` above the documented maximum.
- DO make `=` mean equality; use explicit suffixes or named operators for other semantics, consistently across the API.
- DO use consistent parameter names for common concepts: `page`, `pageSize`, `sort`, `fields`, `expand`, `createdFrom`, `createdTo`.
- DO distinguish date-only and timestamp concepts in names, such as `publishedDate` versus `publishedAt`.
- DO consider a different query model, such as GraphQL, when clients need arbitrary operators across most properties.

**Representations and methods**
- DO use one unified resource representation for `GET by id` and successful `POST`, `PUT`, and `PATCH` responses.
- DO avoid nested data in the canonical resource representation; return IDs for related resources unless the endpoint explicitly expands them.
- DO use `POST` for creation, normally returning `201 Created` and the created resource.
- DO consider `POST` for reads only in exceptional cases, such as sensitive or overly complex filters that do not fit a query string.
- DO use `PATCH` for partial updates, normally returning `200 OK`.
- DO use `PUT` for full replacement, or creation by known identifier when intentionally supported; keep it idempotent.
- DO return a `Location` header when a request creates a resource.

**Non-CRUD interactions**
- DO model interactions as resources instead of verbs or event names.
- DO use `POST` to create interaction resources such as `payments`, `releases`, `player-events`, or `account-activations`.
- DO name pseudoresources after the thing being created, not the action being performed.

**Content negotiation**
- DO use HTTP headers for response format and language negotiation, especially `Accept` and `Accept-Language`.
- DO return `406 Not Acceptable` when the requested representation cannot be produced.
- DO put media type suffixes in URLs only for static assets or UI-linked files outside normal API resource negotiation.

**HATEOAS**
- DO keep resource relationships simple with identifiers such as `authorId`.
- DO skip hypermedia `_links` unless a specific client contract truly needs them; REST APIs usually do not have human interactivity that requires link navigation.

**Contract conventions**
- DO use `camelCase` in JSON contracts.
- DO choose standards for coded values and document them, for example language codes from ISO 639-1.
- DO use meaningful property names such as `isActive`, `createdAt`, and `documentType`.
- DO keep naming consistent across the whole API; choose one pattern such as `startDate`, `dateFrom`, or `dateStart` and stick to it.
- DO group related fields into objects when they belong together: `client.name`, `client.street`, `client.zip`, `client.country`.

**Errors**
- DO use one error structure for all `4xx` and `5xx` responses.
- DO prefer RFC 9457 Problem Details (`application/problem+json`), using .NET `ProblemDetails` where available.
- DO define priority rules for ambiguous failures, such as auth failure vs validation failure vs missing resource vs unacceptable media type.
- DO evaluate content negotiation early enough that an unproducible requested representation consistently returns `406 Not Acceptable`.
- DO include a request identifier or trace reference in error responses when useful for support.

**Versioning**
- DO try to avoid, delay, or minimize API versioning through careful design.
- DO use an explicit version segment such as `/v1/resources` when a versioned public contract is needed.
- DO consider potential future interface changes while designing the contract; a slightly more flexible contract can prevent premature versioning.
- DO prefer backward-compatible changes.
- DO communicate backward-incompatible changes explicitly when they are unavoidable.
- DO consider forced updates or minimum supported client versions for tightly controlled clients, such as mobile or desktop apps, when old clients cannot be supported safely.

**Logging and traceability**
- DO assign a unique server-generated request ID to every HTTP request.
- DO log errors with the request ID and return that ID to the client.
- DO distinguish request-level, trace-level, and correlation-level identifiers.
- DO accept trace or correlation identifiers from clients when appropriate, but generate the request ID server-side.
- DO use telemetry to learn how clients actually use the API before redesigning representations or introducing a new version.

## DON'T

**Resource URLs**
- DON'T use singular resource nouns like `user/{id}` when the API convention is plural.
- DON'T let the API become only a thin public copy of database tables and CRUD operations.
- DON'T put resource identifiers in query parameters for single-resource lookup: avoid `customers?id={id}` for a GET-by-id endpoint.
- DON'T model filtered related collections as multiple URL paths such as `customers/{customerId}/invoices` or `orders/{orderId}/invoices`; use collection query parameters instead.
- DON'T use ambiguous collection-to-collection paths such as `users/invoices`.
- DON'T force clients to provide parent IDs in deeply nested paths when the child resource has a stable unique identifier.
- DON'T model 1:1 related data as nested field-like paths such as `users/{id}/profile` or `users/{id}/image`.
- DON'T expose field-level endpoints such as `users/{id}/name`.
- DON'T use action verbs in resource paths such as `users/{id}/disable`, `articles/{id}/publish`, or `accounts/{id}/activate`.
- DON'T over-nest paths like `resource1/{id1}/resource2/{id2}/resource3/{id3}` unless every level is required to identify the resource.

**Query design**
- DON'T invent one-off query parameter semantics per endpoint.
- DON'T use equality syntax for contains, ranges, or fuzzy matching without making that behavior explicit.
- DON'T hide operators inside parameter values when named parameters or suffixes can express the contract clearly.
- DON'T leave sorting, paging, or filtering behavior implicit in code only; document it in the API contract.
- DON'T silently clamp invalid page sizes or ignore unsupported query parameters when the contract says they are invalid.
- DON'T add `fields` or `expand` automatically to every endpoint; they change response shapes and can complicate caching and client expectations.

**Representations and methods**
- DON'T return different response shapes for `GET by id`, `POST`, `PUT`, and `PATCH` for the same resource.
- DON'T wrap a canonical resource in unnecessary envelope objects unless the API has a deliberate, consistent envelope convention.
- DON'T include large nested objects by default if IDs or explicit expansion are enough.
- DON'T treat `POST`, `PATCH`, and `PUT` as interchangeable update methods.
- DON'T use `POST` for reads unless query-string limits, sensitive filters, or similar constraints justify the trade-off.

**Content negotiation**
- DON'T put response media types in resource URLs, such as `invoices/{id}.xml`, for normal API resources.
- DON'T create format-specific action endpoints such as `invoices/{id}/export` when the same resource plus `Accept` header expresses the requested representation.

**Contracts and errors**
- DON'T mix casing styles in JSON.
- DON'T alternate names for the same concept across endpoints.
- DON'T flatten related object fields into repeated primitive prefixes when an object better represents the domain.
- DON'T add `_links` blocks just to satisfy HATEOAS theory when IDs are enough for the API contract.
- DON'T return ad hoc error bodies that vary by endpoint.
- DON'T omit a documented rule for which error wins when multiple failures apply.

**Versioning and logging**
- DON'T version reflexively for every change.
- DON'T make breaking changes silently.
- DON'T depend on client-supplied request IDs as the unique server request identifier.
- DON'T log errors without enough identifiers for support to connect a client-visible failure with server logs.

