using ReconciliationApp.Frontend.Components;
using ReconciliationApp.Frontend.State;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSingleton<ReconciliationApp.Frontend.State.LayoutState>();
builder.Services.AddScoped<ReconciliationApp.Frontend.State.ReconciliationStore>(); // <- recomendado también (store por circuito)

var app = builder.Build();

// Configure the HTTP request pipeline.
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