# Cash Application Pymes - Reconciliation

Backend en **.NET 8** con arquitectura por capas (**API / Application / Domain / Infrastructure**) para:
- Importación de CSV (deuda y pagos)
- Preview de conciliación
- Persistencia del resultado de conciliación por corrida (run)

## Requisitos
- .NET SDK 8
- PostgreSQL

## Configuración (local)
Este repo no debe contener secretos. Para desarrollo usar **User Secrets**:

```bash
dotnet user-secrets init --project ReconciliationApp.API
dotnet user-secrets set "ConnectionStrings:Default" "Host=...;Database=...;Username=...;Password=..." --project ReconciliationApp.API