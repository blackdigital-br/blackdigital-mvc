using BlackDigital.Mvc.Example.Services;
using BlackDigital.Mvc.Example.Transform;
using BlackDigital.Mvc.Rest;
using BlackDigital.Mvc.Rest.Trasnforms;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// Novo approach com middleware - substitui a geração dinâmica de controllers
builder.Services.AddRestServices(restService =>
    restService.AddService<IUser, UserImplemention>());

// Adiciona as configurações padrão do MVC (model binders, constraints)
builder.Services.AddRestMvcOptions();


builder.Services.AddTransform(transformConfig =>
    transformConfig.AddRule("POST:api/user/", "2025-10-08", new SaveUserTransformRule("input"))
                   .AddRule("POST:api/user/", "2025-10-08", new SaveUserTransformRule("output"), TransformDirection.Output)
                   .AddInputRule<OldSaveUsertoSaveUserTransformRule>("POST:api/user/", "2020-10-10")
                   .AddOutputRule<SaveUserIdTransformRule>("POST:api/user/", "2020-10-10")
);


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

// Adiciona o middleware REST que substitui os controllers dinâmicos
app.UseRestMiddleware();

app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
