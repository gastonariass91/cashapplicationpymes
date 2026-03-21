namespace ReconciliationApp.Domain.Enums;

public enum ReconciliationMode
{
    Validation = 0,   // El usuario revisa y aprueba cada match (default)
    Automatic  = 1    // Los matches que superen el threshold se aplican solos
}
