using BlackDigital.AspNet.Example.Services;
using BlackDigital.AspNet.Example.Transform;
using BlackDigital.AspNet.OpenApi;
using BlackDigital.AspNet.Rest;
using BlackDigital.AspNet.Rest.Transforms;
using BlackDigital.AspNet.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// Required by BdHttpContextHelper / IHttpContextAccessor consumers.
builder.Services.AddHttpContextAccessor();

// Registers ITokenService (BlackDigital.Domain.Security.JwtTokenService) reading
// the "Jwt" section. It fails fast at startup when SecretKey/Issuer/Audience are
// missing, so production must supply Jwt__SecretKey (>= 32 bytes) via env vars.
builder.Services.AddJwtTokenService(builder.Configuration);

// JWT bearer authentication: turns the incoming "Authorization: Bearer <token>"
// header into HttpContext.User, which is what the REST middleware inspects.
var jwt = builder.Configuration.GetSection("Jwt");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwt["Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["SecretKey"]!)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

// Named policies referenced by [Authorize(Policy = "...")] on the implementations.
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Users.Read", policy => policy.RequireRole("Admin", "Manager"));
    options.AddPolicy("Users.Write", policy => policy.RequireRole("Admin"));
});

// Novo approach com middleware - substitui a geração dinâmica de controllers
builder.Services.AddRestServices(restService =>
    restService.AddService<IUser, UserImplementation>()
               .AddService<IAuth, AuthImplementation>()
               .AddService<ISecurityDemo, SecurityDemoImplementation>()
               .AddService<IAdminArea, AdminAreaImplementation>());

// Adiciona as configurações padrão do MVC (model binders, constraints)
builder.Services.AddRestMvcOptions();


builder.Services.AddTransform(transformConfig =>
    transformConfig.AddRule("POST:api/user/", "2025-10-08", new SaveUserTransformRule("input"))
                   .AddRule("POST:api/user/", "2025-10-08", new SaveUserTransformRule("output"), TransformDirection.Output)
                   .AddOutputRule<SaveUserIdTransformRule>("POST:api/user/", "2020-10-10")
);



builder.Services.AddOpenApi(options =>
{
    // --- Caller-controlled settings (no more hardcoded strings in the
    // --- OpenAPI generator itself). Everything below lives in the host,
    // --- not in the reusable generator code.
    options.Title = "My Project API";
    options.Version = "v1";
    options.Description = "Template base for BlackDigital-based projects. Auto-generated via Microsoft.OpenApi + Scalar integration — no Swashbuckle.";
    options.Contact = new global::Microsoft.OpenApi.OpenApiContact
    {
        Name = "TioRAC",
        Url = new Uri("https://github.com/TioRAC"),
        Email = "tio.rac@tiorac.dev"
    };
    options.License = new global::Microsoft.OpenApi.OpenApiLicense
    {
        Name = "MIT",
        Url = new Uri("https://opensource.org/licenses/MIT")
    };
    options.DocumentName = "v1";

    // Expose the feature only on non-Prod environments. Previously the
    // loader had this baked in; now it belongs to the host configuration.
    options.Enabled = builder.Environment.IsDevelopment() || builder.Environment.IsStaging();

    // --- Assembly discovery (replaces old "MyProject" prefix hardcode) ---
    // When IncludedAssemblyPrefixes is empty, every non-BCL assembly is
    // scanned; for templates we want the stricter behavior: limit to our
    // product assemblies plus any other BD-specific module.
    options.IncludedAssemblyPrefixes.Add("BlackDigital.AspNet.Example");

    // The default exclusions drop every "BlackDigital.AspNet*" assembly, which
    // would also hide this example's own services — remove it so they are
    // discovered and shown in Scalar.
    options.ExcludedAssemblyPrefixes.Remove("BlackDigital.AspNet");

    // Optional: tighten or relax exclusions on top of the BCL defaults.
    // options.ExcludedAssemblyPrefixes.Add("MyProject.Other.Shared");

    // --- Security: default Bearer JWT (pre-populated by options defaults) ---
    // For non-trivial projects you can override options.SecuritySchemes here
    // or call options.SecuritySchemes.Remove("Bearer") to opt out entirely.
    options.ApplyBearerAsGlobalSecurityRequirement = true;
    options.PreferredSecuritySchemes.Clear();
    options.PreferredSecuritySchemes.Add("Bearer");

    // --- Scalar UI tuning ---
    options.ScalarTitle = $"My Project API - {options.Version}";
    options.OpenApiRoutePattern = "/openapi/{documentName}.json";
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Populates HttpContext.User from the Bearer token. It MUST run before the REST
// middleware, otherwise [Authorize] would always see an anonymous user.
app.UseAuthentication();

// Adiciona o middleware REST que substitui os controllers dinâmicos
app.UseRestMiddleware();

app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();
app.MapOpenApi();
app.MapFallbackToFile("index.html");

app.Run();
