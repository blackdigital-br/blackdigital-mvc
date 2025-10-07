using BlackDigital.Mvc.Binder;
using BlackDigital.Mvc.Constraint;
using BlackDigital.Mvc.Example.Services;
using BlackDigital.Mvc.Rest;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// Novo approach com middleware - substitui a geração dinâmica de controllers
builder.Services.AddRestServices(restService =>
    restService.AddService<IUser, UserImplemention>());

// Adiciona as configurações padrão do MVC (model binders, constraints)
builder.Services.AddRestMvcOptions();



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
