# Arquitectura

## Capas
- API: endpoints y wiring (DI, swagger, middlewares)
- Application: casos de uso (handlers)
- Domain: entidades y reglas
- Infrastructure: EF Core, DbContext, migraciones y repositorios

## Regla
La API no debe tocar DbContext para lógica de negocio: solo orquesta.
