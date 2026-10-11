# Transformation System Reference

## Purpose

The transformation system lets APIs evolve while keeping backward compatibility. It transforms request bodies (input) and response bodies (output) based on a client-requested API version, applying rules sequentially.

## Registration

```csharp
builder.Services.AddTransform(transform =>
{
    // Input: old clients send V1, service expects V2
    transform.AddInputRule<UserV1ToV2Rule>("POST:api/users/", "2024-01-01");

    // Output: service returns V2, old clients expect V1
    transform.AddOutputRule<UserV2ToV1Rule>("GET:api/users/", "2024-01-01");
    transform.AddOutputRule<UserV2ToV1Rule>("POST:api/users/", "2024-01-01");

    // Same rule for both directions
    transform.AddInputAndOutputRule<NormalizeRule>("PUT:api/users/", "2024-06-01");
});
```

`AddTransform` registers a `TransformBuilder` singleton and a `TransformManager` scoped service.

## Transform key

Each rule is identified by a `TransformKey` with three parts:

```
TransformKey(actionKey, version, direction)
```

- **actionKey**: `"{HTTP_METHOD}:{BaseRoute}/{ActionRoute}"` (e.g. `"POST:api/users/"`)
- **version**: a string compared with ordinal comparison (typically a date like `"2024-01-01"`)
- **direction**: `Input`, `Output`, or `Both` (a `[Flags]` enum)

A rule registered with `Both` matches both `Input` and `Output` queries.

## Version resolution per request

For each request, the middleware resolves the requested version from:

1. query string `?api-version=2024-01-01`
2. header `x-api-version: 2024-01-01`
3. fallback: `1970-01-01`

## Rule selection and ordering

`TransformManager.GetRequiredRules(key)` returns rules whose:
- `Key` matches the action key
- `(rule.Direction & requestedDirection) == requestedDirection` (so `Both` matches both)
- `Version` is **greater than** the requested version (ordinal string comparison)

Rules are then ordered:
- **Input**: ascending by version (oldest → newest)
- **Output**: descending by version (newest → oldest)

This means a client requesting version `2024-01-01` receives all transformations registered for versions newer than `2024-01-01`. A client requesting `1970-01-01` (the default) receives **all** registered transformations.

## Rule implementation

Rules implement `ITransformRule`. The recommended base classes are:

```csharp
public class UserV1ToV2Rule : TransformRule<UserV1, UserV2>
{
    public override UserV2? Transform(UserV1? source)
    {
        if (source == null) return null;
        return new UserV2
        {
            Id = source.Id,
            FullName = $"{source.FirstName} {source.LastName}",
            Email = source.Email
        };
    }
}
```

`TransformRule<TIn, TOut>` sets `InputType = typeof(TIn)` and `OutputType = typeof(TOut)` and provides both sync and async overloads (the async one calls the sync one by default).

`TransformRule<T>` is for same-type transformations; the non-generic `TransformRule` passes the value through unchanged.

### Input type resolution

For input transformations, the middleware calls `transformManager.GetFirstInputType(key)` to determine which type to deserialize the request body into. This is the `InputType` of the first matching rule. When no rule matches, the body is deserialized into the parameter's declared type.

## Builder API

`TransformBuilder` offers several overloads:

- `AddRule(TransformKey, ITransformRule)` — instance
- `AddRule(TransformKey, Type)` — type, instantiated via DI
- `AddRule<TRule>(TransformKey)` — generic, instantiated via DI
- `AddInputRule(...)`, `AddOutputRule(...)`, `AddInputAndOutputRule(...)` — convenience overloads taking `(string key, string version, ...)`
- `AddRule(ITransformRule, params TransformKey[])` / `AddRule(Type, params TransformKey[])` / `AddRule<TRule>(params TransformKey[])` — one rule for multiple keys

Type-based rules are instantiated through `ActivatorUtilities.CreateInstance`, so they support constructor dependency injection.

## Source files

- `src/Rest/Transforms/TransformManager.cs`
- `src/Rest/Transforms/TransformBuilder.cs`
- `src/Rest/Transforms/TransformKey.cs`
- `src/Rest/Transforms/TransformDirection.cs`
- `src/Rest/Transforms/TransformDirectionHelper.cs`
- `src/Rest/Transforms/TransformRule.cs`
- `src/Rest/Transforms/ITransformRule.cs`
- `src/Rest/Transforms/TransformExtensions.cs`
