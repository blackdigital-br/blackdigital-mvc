# Documentation Reference

## Goal

Keep examples and docs aligned with the real API surface of BlackDigital.AspNet.

## Documentation rules

1. Read the relevant implementation files before editing docs.
2. Treat `src/` as authoritative when there is any mismatch.
3. Keep `README.md` and `README.pt.md` synchronized for core examples.
4. Keep `doc/*.md` and `doc/*.pt.md` synchronized when the same topic exists in both languages.
5. Prefer examples that compile against the current public API.
6. When a convention is surprising, explain it directly instead of hiding it.

## Known mismatch patterns to watch for

- Controller-based examples instead of `[Service]`/`[Action]` interface-based services
- `UseRestMiddleware()` placed after `UseAuthorization()` (must be before)
- `[Query]` examples that only show simple scalars and ignore complex-object expansion
- Forgetting that `BusinessException` maps to its own HTTP status code
- Hardcoding the Bearer security scheme or the OpenAPI title instead of using `BdOpenApiOptions`
- JWT secrets shorter than 32 bytes (the constructor rejects them)
- Transformation rules that do not extend `TransformRule<TIn, TOut>`
- Transformation keys that do not match the `"{METHOD}:{BaseRoute}/{ActionRoute}"` pattern
- Email template paths outside `{contentRoot}/templates/email/`
- `AddRestMvcOptions()` combined with a separate `AddControllers()` call (it calls `AddControllers` internally)
- Omitting `DataBuilder`/`Id`/`JsonCast` from the core `BlackDigital` library when cross-referencing

## Recommended update flow

1. inspect source files
2. inspect the matching docs
3. fix incorrect examples first
4. expand coverage second
5. verify both English and Portuguese versions when needed

## File map

- root guides
  - `README.md`
  - `README.pt.md`
- detailed docs
  - `doc/rest-system*.md`
  - `doc/transform-system*.md`
  - `doc/openapi*.md`
  - `doc/security*.md`
  - `doc/email*.md`
- runnable example
  - `example/Program.cs`
  - `example/Services/`
  - `example/Transform/`
