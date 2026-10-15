using BlackDigital.AspNet.OpenApi;
using Microsoft.OpenApi;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace BlackDigital.AspNet.Test.OpenApi
{
    public class BdOpenApiSchemaGeneratorTest
    {
        private static BdOpenApiSchemaGenerator CreateGenerator() => new();

        private enum TestEnum { ValueA, ValueB }

        private class SimplePoco
        {
            public string? Name { get; set; }
            public int Age { get; set; }
            public bool IsActive { get; set; }
        }

        private class NestedPoco
        {
            public string? Title { get; set; }
            public SimplePoco? Child { get; set; }
        }

        private class SelfReferencing
        {
            public string? Name { get; set; }
            public SelfReferencing? Next { get; set; }
        }

        private class WithAttributes
        {
            [JsonPropertyName("custom_name")]
            public string? FirstName { get; set; }

            [DataMember(Name = "data_member_name")]
            public string? LastName { get; set; }
        }

        [Fact]
        public void GetSchema_ForBoolean_ShouldReturnBooleanType()
        {
            var gen = CreateGenerator();

            var schema = gen.GetSchema(typeof(bool));

            var openApiSchema = Assert.IsType<OpenApiSchema>(schema);
            Assert.Equal(JsonSchemaType.Boolean, openApiSchema.Type);
        }

        [Theory]
        [InlineData(typeof(int), JsonSchemaType.Integer, "int32")]
        [InlineData(typeof(long), JsonSchemaType.Integer, "int64")]
        [InlineData(typeof(short), JsonSchemaType.Integer, "int32")]
        [InlineData(typeof(byte), JsonSchemaType.Integer, "int32")]
        [InlineData(typeof(uint), JsonSchemaType.Integer, "int32")]
        [InlineData(typeof(ulong), JsonSchemaType.Integer, "int64")]
        public void GetSchema_ForIntegerTypes_ShouldReturnIntegerWithFormat(Type type, JsonSchemaType expectedType, string expectedFormat)
        {
            var gen = CreateGenerator();

            var schema = gen.GetSchema(type);

            var openApiSchema = Assert.IsType<OpenApiSchema>(schema);
            Assert.Equal(expectedType, openApiSchema.Type);
            Assert.Equal(expectedFormat, openApiSchema.Format);
        }

        [Theory]
        [InlineData(typeof(float), "float")]
        [InlineData(typeof(double), "double")]
        public void GetSchema_ForFloatingTypes_ShouldReturnNumberWithFormat(Type type, string expectedFormat)
        {
            var gen = CreateGenerator();

            var schema = gen.GetSchema(type);

            var openApiSchema = Assert.IsType<OpenApiSchema>(schema);
            Assert.Equal(JsonSchemaType.Number, openApiSchema.Type);
            Assert.Equal(expectedFormat, openApiSchema.Format);
        }

        [Fact]
        public void GetSchema_ForDecimal_ShouldReturnNumber()
        {
            var gen = CreateGenerator();

            var schema = gen.GetSchema(typeof(decimal));

            var openApiSchema = Assert.IsType<OpenApiSchema>(schema);
            Assert.Equal(JsonSchemaType.Number, openApiSchema.Type);
            Assert.Null(openApiSchema.Format);
        }

        [Fact]
        public void GetSchema_ForString_ShouldReturnString()
        {
            var gen = CreateGenerator();

            var schema = gen.GetSchema(typeof(string));

            var openApiSchema = Assert.IsType<OpenApiSchema>(schema);
            Assert.Equal(JsonSchemaType.String, openApiSchema.Type);
        }

        [Fact]
        public void GetSchema_ForChar_ShouldReturnString()
        {
            var gen = CreateGenerator();

            var schema = gen.GetSchema(typeof(char));

            var openApiSchema = Assert.IsType<OpenApiSchema>(schema);
            Assert.Equal(JsonSchemaType.String, openApiSchema.Type);
        }

        [Fact]
        public void GetSchema_ForDateTime_ShouldReturnStringWithDateTimeFormat()
        {
            var gen = CreateGenerator();

            var schema = gen.GetSchema(typeof(DateTime));

            var openApiSchema = Assert.IsType<OpenApiSchema>(schema);
            Assert.Equal(JsonSchemaType.String, openApiSchema.Type);
            Assert.Equal("date-time", openApiSchema.Format);
        }

        [Theory]
        [InlineData(typeof(DateTimeOffset), "date-time")]
        [InlineData(typeof(Guid), "uuid")]
        [InlineData(typeof(Uri), "uri")]
        [InlineData(typeof(DateOnly), "date")]
        [InlineData(typeof(TimeOnly), "time")]
        [InlineData(typeof(byte[]), "byte")]
        public void GetSchema_ForWellKnownTypes_ShouldReturnStringWithFormat(Type type, string expectedFormat)
        {
            var gen = CreateGenerator();

            var schema = gen.GetSchema(type);

            var openApiSchema = Assert.IsType<OpenApiSchema>(schema);
            Assert.Equal(JsonSchemaType.String, openApiSchema.Type);
            Assert.Equal(expectedFormat, openApiSchema.Format);
        }

        [Fact]
        public void GetSchema_ForEnum_ShouldReturnString()
        {
            var gen = CreateGenerator();

            var schema = gen.GetSchema(typeof(TestEnum));

            var openApiSchema = Assert.IsType<OpenApiSchema>(schema);
            Assert.Equal(JsonSchemaType.String, openApiSchema.Type);
        }

        [Fact]
        public void GetSchema_ForObject_ShouldReturnEmptySchema()
        {
            var gen = CreateGenerator();

            var schema = gen.GetSchema(typeof(object));

            var openApiSchema = Assert.IsType<OpenApiSchema>(schema);
            // free-form {} — no Type is set
            Assert.Null(openApiSchema.Properties);
        }

        [Fact]
        public void GetSchema_ForNullableValueType_ShouldWrapInOneOf()
        {
            var gen = CreateGenerator();

            var schema = gen.GetSchema(typeof(int?));

            var openApiSchema = Assert.IsType<OpenApiSchema>(schema);
            Assert.NotNull(openApiSchema.OneOf);
            Assert.Equal(2, openApiSchema.OneOf.Count);
            // First is the core type, second is null
            var core = Assert.IsType<OpenApiSchema>(openApiSchema.OneOf[0]);
            Assert.Equal(JsonSchemaType.Integer, core.Type);
            var nullSchema = Assert.IsType<OpenApiSchema>(openApiSchema.OneOf[1]);
            Assert.Equal(JsonSchemaType.Null, nullSchema.Type);
        }

        [Fact]
        public void GetSchema_ForArray_ShouldReturnArrayWithItems()
        {
            var gen = CreateGenerator();

            var schema = gen.GetSchema(typeof(int[]));

            var openApiSchema = Assert.IsType<OpenApiSchema>(schema);
            Assert.Equal(JsonSchemaType.Array, openApiSchema.Type);
            Assert.NotNull(openApiSchema.Items);
            var itemSchema = Assert.IsType<OpenApiSchema>(openApiSchema.Items);
            Assert.Equal(JsonSchemaType.Integer, itemSchema.Type);
        }

        [Fact]
        public void GetSchema_ForList_ShouldReturnArrayWithItems()
        {
            var gen = CreateGenerator();

            var schema = gen.GetSchema(typeof(List<string>));

            var openApiSchema = Assert.IsType<OpenApiSchema>(schema);
            Assert.Equal(JsonSchemaType.Array, openApiSchema.Type);
            Assert.NotNull(openApiSchema.Items);
        }

        [Fact]
        public void GetSchema_ForDictionary_ShouldReturnObjectWithAdditionalProperties()
        {
            var gen = CreateGenerator();

            var schema = gen.GetSchema(typeof(Dictionary<string, int>));

            var openApiSchema = Assert.IsType<OpenApiSchema>(schema);
            Assert.Equal(JsonSchemaType.Object, openApiSchema.Type);
            Assert.NotNull(openApiSchema.AdditionalProperties);
            var valueSchema = Assert.IsType<OpenApiSchema>(openApiSchema.AdditionalProperties);
            Assert.Equal(JsonSchemaType.Integer, valueSchema.Type);
        }

        [Fact]
        public void GetSchema_ForPoco_ShouldReturnReferenceAndRegisterComponent()
        {
            var gen = CreateGenerator();

            var schema = gen.GetSchema(typeof(SimplePoco));

            Assert.IsType<OpenApiSchemaReference>(schema);
            var components = gen.GetComponentSchemas();
            Assert.NotEmpty(components);
        }

        [Fact]
        public void GetSchema_ForPoco_ShouldRegisterAllProperties()
        {
            var gen = CreateGenerator();

            gen.GetSchema(typeof(SimplePoco));
            var components = gen.GetComponentSchemas();
            var component = Assert.Single(components.Values);
            var pocoSchema = Assert.IsType<OpenApiSchema>(component);

            Assert.NotNull(pocoSchema.Properties);
            Assert.Contains("name", pocoSchema.Properties.Keys);
            Assert.Contains("age", pocoSchema.Properties.Keys);
            Assert.Contains("isActive", pocoSchema.Properties.Keys);
        }

        [Fact]
        public void GetSchema_ForPoco_ShouldMarkNonNullableValueTypesAsRequired()
        {
            var gen = CreateGenerator();

            gen.GetSchema(typeof(SimplePoco));
            var components = gen.GetComponentSchemas();
            var pocoSchema = Assert.IsType<OpenApiSchema>(Assert.Single(components.Values));

            Assert.NotNull(pocoSchema.Required);
            Assert.Contains("age", pocoSchema.Required);
            Assert.Contains("isActive", pocoSchema.Required);
            // name is string (nullable reference type), not required
            Assert.DoesNotContain("name", pocoSchema.Required);
        }

        [Fact]
        public void GetSchema_ForNestedPoco_ShouldRegisterBothComponents()
        {
            var gen = CreateGenerator();

            gen.GetSchema(typeof(NestedPoco));
            var components = gen.GetComponentSchemas();

            Assert.Equal(2, components.Count);
        }

        [Fact]
        public void GetSchema_ForSelfReferencingType_ShouldNotStackOverflow()
        {
            var gen = CreateGenerator();

            var schema = gen.GetSchema(typeof(SelfReferencing));

            Assert.NotNull(schema);
        }

        [Fact]
        public void GetSchema_WithJsonPropertyName_ShouldUseCustomName()
        {
            var gen = CreateGenerator();

            gen.GetSchema(typeof(WithAttributes));
            var components = gen.GetComponentSchemas();
            var pocoSchema = Assert.IsType<OpenApiSchema>(Assert.Single(components.Values));

            Assert.Contains("custom_name", pocoSchema.Properties.Keys);
        }

        [Fact]
        public void GetSchema_WithDataMember_ShouldUseCustomName()
        {
            var gen = CreateGenerator();

            gen.GetSchema(typeof(WithAttributes));
            var components = gen.GetComponentSchemas();
            var pocoSchema = Assert.IsType<OpenApiSchema>(Assert.Single(components.Values));

            Assert.Contains("data_member_name", pocoSchema.Properties.Keys);
        }

        [Fact]
        public void GetComponentSchemas_WhenNoTypesGenerated_ShouldReturnEmpty()
        {
            var gen = CreateGenerator();

            var components = gen.GetComponentSchemas();

            Assert.Empty(components);
        }

        [Fact]
        public void GetSchema_ForIEnumerable_ShouldReturnArray()
        {
            var gen = CreateGenerator();

            var schema = gen.GetSchema(typeof(IEnumerable<int>));

            var openApiSchema = Assert.IsType<OpenApiSchema>(schema);
            Assert.Equal(JsonSchemaType.Array, openApiSchema.Type);
        }

        [Fact]
        public void GetSchema_ForHashSet_ShouldReturnArray()
        {
            var gen = CreateGenerator();

            var schema = gen.GetSchema(typeof(HashSet<string>));

            var openApiSchema = Assert.IsType<OpenApiSchema>(schema);
            Assert.Equal(JsonSchemaType.Array, openApiSchema.Type);
        }
    }
}
