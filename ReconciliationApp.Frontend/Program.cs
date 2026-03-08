using ReconciliationApp.Frontend.Components;
using ReconciliationApp.Frontend.Services;
using ReconciliationApp.Frontend.State;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSingleton<LayoutState>();
builder.Services.AddScoped<ReconciliationStore>();

builder.Services.AddHttpClient<ReconciliationApiClient>(client =>
{
    client.BaseAddress = new Uri("http://localhost:5081/");
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
