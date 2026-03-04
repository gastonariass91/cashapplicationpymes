# Desarrollo

## DB / Supabase desde Codespaces
Supabase Direct connection puede no ser IPv4 compatible. Desde Codespaces usar Pooler.
Ejemplo de usuario con tenant:
Username=postgres.<project_ref>

## EF
dotnet tool restore
dotnet ef database update --project ReconciliationApp.Infrastructure --startup-project ReconciliationApp.API
