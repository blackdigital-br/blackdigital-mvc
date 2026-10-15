using System.Reflection;
using System.Runtime.Serialization;
using Microsoft.OpenApi;

namespace BlackDigital.AspNet.OpenApi
{
    /// <summary>
    /// Reflection-based schema generator that converts any .NET Type into an
    /// <see cref="IOpenApiSchema"/> using only the public Microsoft.OpenApi
    /// object model (no Microsoft.AspNetCore.OpenApi / Swashbuckle dependency).
    /// Works against the Microsoft.OpenApi 3.x APIs where:
    ///
    /// - Type is the enum <see cref="JsonSchemaType"/> (not a string).
    /// - There is no .Nullable setter on OpenApiSchema; nullable unions are
    ///   expressed via .OneOf with a JsonSchemaType.Null leaf.
    /// - References are emitted as <see cref="OpenApiSchemaReference"/> (it
    ///   implements IOpenApiSchema and is written as a $ref by the writer).
    ///
    /// Handles primitives, nullables, collections, string-keyed dictionaries,
    /// POCOs/records and cycles with component-level caching.
    /// </summary>
    public class BdOpenApiSchemaGenerator
    {
        // Host document passed to OpenApiSchemaReference ctors. The constructor
        // does not actually mutate it; it only uses it for logical ownership
        // context when resolving reference IDs at serialisation time. Kept as
        // an instance field so each generator owns its own context instead of
        // sharing static state across concurrent document builds.
        internal readonly OpenApiDocument HostDoc = new();

        private readonly Dictionary<Type, OpenApiSchema> _componentByType = new();
        private readonly List<(OpenApiSchema schema, string name)> _components = new();
        private readonly Dictionary<OpenApiSchema, int> _componentBySlotIndex = new();

        /// <summary>Components keyed by their stable $ref ID.</summary>
        /// <returns>A read-only dictionary mapping component names to their schemas.</returns>
        public IReadOnlyDictionary<string, IOpenApiSchema> GetComponentSchemas()
        {
            var result = new Dictionary<string, IOpenApiSchema>();
            for (var i = 0; i < _components.Count; i++)
            {
                var (s, name) = _components[i];
                if (!string.IsNullOrEmpty(name))
                    result[name] = s;
            }
            return result;
        }

        /// <summary>
        /// Produces a schema for <paramref name="type"/>, wrapping the non-null
        /// core in a OneOf [ core, null ] union when the type is nullable.
        /// </summary>
        /// <param name="type">The .NET type to generate a schema for.</param>
        /// <returns>An <see cref="IOpenApiSchema"/> describing the type.</returns>
        public IOpenApiSchema GetSchema(Type type)
        {
            var t = Nullable.GetUnderlyingType(type) ?? type;
            var nullable = t != type;
            var core = BuildCoreSchema(t);
            if (!nullable)
                return core;
            return WrapNullable(core);
        }

        // ---------------------------------------------------------------
        // Core (non-nullable) schema dispatch.
        // ---------------------------------------------------------------
        private IOpenApiSchema BuildCoreSchema(Type t)
        {
            if (t.IsEnum)
                return new OpenApiSchema { Type = JsonSchemaType.String };

            switch (Type.GetTypeCode(t))
            {
                case TypeCode.Boolean:
                    return new OpenApiSchema { Type = JsonSchemaType.Boolean };
                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                    return new OpenApiSchema { Type = JsonSchemaType.Integer, Format = "int32" };
                case TypeCode.Int64:
                case TypeCode.UInt64:
                    return new OpenApiSchema { Type = JsonSchemaType.Integer, Format = "int64" };
                case TypeCode.Single:
                    return new OpenApiSchema { Type = JsonSchemaType.Number, Format = "float" };
                case TypeCode.Double:
                    return new OpenApiSchema { Type = JsonSchemaType.Number, Format = "double" };
                case TypeCode.Decimal:
                    return new OpenApiSchema { Type = JsonSchemaType.Number };
                case TypeCode.Char:
                case TypeCode.String:
                    return new OpenApiSchema { Type = JsonSchemaType.String };
                case TypeCode.DateTime:
                    return new OpenApiSchema { Type = JsonSchemaType.String, Format = "date-time" };
            }

            // Non-TypeCode well-known BCL types.
            if (t == typeof(DateTimeOffset)) return new OpenApiSchema { Type = JsonSchemaType.String, Format = "date-time" };
            if (t == typeof(Guid)) return new OpenApiSchema { Type = JsonSchemaType.String, Format = "uuid" };
            if (t == typeof(Uri)) return new OpenApiSchema { Type = JsonSchemaType.String, Format = "uri" };
            if (t == typeof(DateOnly)) return new OpenApiSchema { Type = JsonSchemaType.String, Format = "date" };
            if (t == typeof(TimeOnly)) return new OpenApiSchema { Type = JsonSchemaType.String, Format = "time" };
            if (t == typeof(byte[])) return new OpenApiSchema { Type = JsonSchemaType.String, Format = "byte" };
            if (t == typeof(object)) return new OpenApiSchema(); // free-form {}

            // --- String-keyed dictionaries → OpenAPI object with additionalProperties ---
            if (IsGenericDict(t, out var dictValue))
            {
                return new OpenApiSchema
                {
                    Type = JsonSchemaType.Object,
                    AdditionalProperties = GetSchema(dictValue)
                };
            }

            // --- Enumerables / collections ---
            var elem = ExtractEnumerableElement(t);
            if (elem != null)
            {
                return new OpenApiSchema
                {
                    Type = JsonSchemaType.Array,
                    Items = GetSchema(elem)
                };
            }

            // --- Complex POCOs, records, structs, interfaces ---
            if (t.IsClass || t.IsValueType || t.IsInterface)
                return BuildObjectSchema(t);

            return new OpenApiSchema { Type = JsonSchemaType.String };
        }

        // ---------------------------------------------------------------
        // POCO / class schema.
        // ---------------------------------------------------------------
        private IOpenApiSchema BuildObjectSchema(Type objectType)
        {
            // Cache hit — return a reference to the already-registered component.
            // The component slot is always assigned before property recursion, so
            // the name is stable at this point (or will be assigned later but the
            // index into _components does not change anymore).
            if (_componentByType.TryGetValue(objectType, out var cached))
            {
                var idx = _componentBySlotIndex[cached];
                var name = _components[idx].name;
                if (string.IsNullOrEmpty(name))
                {
                    // Forward reference (nested recursion) — the owning call will
                    // assign the name after it finishes visiting properties. Use a
                    // stable, final-free reference id.
                    name = (cached.Title ?? "Schema") + "_" + idx.ToStringInvariant();
                }
                return new OpenApiSchemaReference(name, HostDoc);
            }

            var schema = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Title = SimplifyName(objectType)
            };
            _componentByType[objectType] = schema;

            // =================================================================
            // IMPORTANT — capture the slot index BEFORE recursing into properties,
            // because nested types will append to _components and shift what
            // `_components.Count - 1` means. Keeping our own index guarantees the
            // same schema never gets two different IDs (which was the root cause
            // of duplicated "Foo_7eeccde5" style entries in the UI).
            // =================================================================
            var slotIdx = _components.Count;
            _components.Add((schema, ""));
            _componentBySlotIndex[schema] = slotIdx;
            var finalName = (schema.Title ?? "Schema") + "_" + slotIdx.ToStringInvariant();
            // Pre-commit the name so forward references during recursion resolve
            // to the correct stable ID even while the property tree is building.
            _components[slotIdx] = (schema, finalName);

            IDictionary<string, IOpenApiSchema> props = new Dictionary<string, IOpenApiSchema>();
            var required = new HashSet<string>();

            var properties = objectType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var p in properties)
            {
                if (!p.CanRead) continue;
                if (p.GetIndexParameters().Length > 0) continue;

                var name = JsonName(p);
                var propSchema = GetSchema(p.PropertyType);
                props[name] = propSchema;

                var propType = p.PropertyType;
                var underlying = Nullable.GetUnderlyingType(propType);
                var isNonNullableValueType = underlying == null
                                             && propType.IsValueType
                                             && !propType.IsClass
                                             && !propType.IsInterface;
                if (isNonNullableValueType)
                    required.Add(name);
            }

            if (props.Count > 0) schema.Properties = props;
            if (required.Count > 0) schema.Required = required;

            // The slot already has its final name assigned; nothing else to do.
            return new OpenApiSchemaReference(finalName, HostDoc);
        }

        // ---------------------------------------------------------------
        // Internals
        // ---------------------------------------------------------------
        private static IOpenApiSchema WrapNullable(IOpenApiSchema core)
        {
            var oneOf = new List<IOpenApiSchema>(capacity: 2)
        {
            core,
            new OpenApiSchema { Type = JsonSchemaType.Null }
        };
            return new OpenApiSchema { OneOf = oneOf };
        }

        private static bool IsGenericDict(Type t, out Type valueType)
        {
            valueType = null!;
            if (t.IsGenericType)
            {
                var def = t.GetGenericTypeDefinition();
                if (def == typeof(IDictionary<,>) || def == typeof(Dictionary<,>) ||
                    def == typeof(IReadOnlyDictionary<,>))
                {
                    var args = t.GetGenericArguments();
                    if (args[0] == typeof(string))
                    {
                        valueType = args[1];
                        return true;
                    }
                }
            }
            foreach (var iface in t.GetInterfaces())
            {
                if (!iface.IsGenericType) continue;
                var def = iface.GetGenericTypeDefinition();
                if (def == typeof(IDictionary<,>) || def == typeof(IReadOnlyDictionary<,>))
                {
                    var args = iface.GetGenericArguments();
                    if (args[0] == typeof(string))
                    {
                        valueType = args[1];
                        return true;
                    }
                }
            }
            return false;
        }

        private static Type? ExtractEnumerableElement(Type t)
        {
            if (t.IsArray) return t.GetElementType();

            if (t.IsGenericType)
            {
                var def = t.GetGenericTypeDefinition();
                if (def == typeof(IEnumerable<>) || def == typeof(ICollection<>) ||
                    def == typeof(IList<>) || def == typeof(List<>) ||
                    def == typeof(IReadOnlyList<>) || def == typeof(IReadOnlyCollection<>) ||
                    def == typeof(HashSet<>))
                {
                    return t.GetGenericArguments()[0];
                }
            }

            foreach (var iface in t.GetInterfaces())
            {
                if (!iface.IsGenericType) continue;
                if (iface.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                    return iface.GetGenericArguments()[0];
            }

            return null;
        }

        private static string JsonName(PropertyInfo p)
        {
            var propName = p.Name;
            try
            {
                var jsonAttr = p.GetCustomAttributes(false)
                    .FirstOrDefault(a => a.GetType().FullName == "System.Text.Json.Serialization.JsonPropertyNameAttribute");
                if (jsonAttr != null)
                {
                    var prop = jsonAttr.GetType().GetProperty("Name") ?? jsonAttr.GetType().GetProperty("Value");
                    if (prop?.GetValue(jsonAttr) is string s && !string.IsNullOrWhiteSpace(s))
                        return s;
                }

                var dataMember = p.GetCustomAttribute<DataMemberAttribute>();
                if (!string.IsNullOrWhiteSpace(dataMember?.Name))
                    return dataMember.Name;
            }
            catch
            {
                // ignored
            }
            return ToCamelCase(propName);
        }

        private static string ToCamelCase(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            if (s.Length == 1) return s.ToLowerInvariant();
            return char.ToLowerInvariant(s[0]) + s.Substring(1);
        }

        private static string SimplifyName(Type t)
        {
            if (!t.IsGenericType) return t.Name;
            var clean = t.Name;
            var backtick = clean.IndexOf('`');
            if (backtick > 0) clean = clean.Substring(0, backtick);
            var args = t.GetGenericArguments();
            return clean + "Of" + string.Join("And", args.Select(SimplifyName));
        }

    }
}
