using ReconciliationApp.Frontend.Components;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ReconciliationApp.Frontend.State.LayoutState>();
// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
;

builder.Services.AddSingleton<ReconciliationApp.Frontend.State.ReconciliationStore>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// app.UseHttpsRedirection(); // disabled for codespaces

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
;

app.Run();
