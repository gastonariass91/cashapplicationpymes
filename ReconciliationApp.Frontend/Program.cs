using ReconciliationApp.Frontend.Components;
using ReconciliationApp.Frontend.Services;
using ReconciliationApp.Frontend.State;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Estado global
builder.Services.AddSingleton<LayoutState>();
builder.Services.AddScoped<ReconciliationStore>();
builder.Services.AddScoped<AuthState>();

// HTTP clients
var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5081";

builder.Services.AddHttpClient<ReconciliationApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
});

builder.Services.AddHttpClient<AuthApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

// app.UseHttpsRedirection(); // disabled for codespaces

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
