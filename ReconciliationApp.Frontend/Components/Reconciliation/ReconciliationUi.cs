using ReconciliationApp.Frontend.Models;

namespace ReconciliationApp.Frontend.Components.Reconciliation;

public static class ReconciliationUi
{
    public static string TypeLabel(ReconMatchType t) => t switch
    {
        ReconMatchType.Exact => "Exacta",
        ReconMatchType.Partial => "Parcial",
        ReconMatchType.Multi => "Multi",
        ReconMatchType.Dup => "Duplicado",
        ReconMatchType.Amb => "Ambigua",
        _ => "Sin match"
    };

    public static string TypeIcon(ReconMatchType t) => t switch
    {
        ReconMatchType.Exact => "◎",
        ReconMatchType.Partial => "◐",
        ReconMatchType.Multi => "≣",
        ReconMatchType.Dup => "⧉",
        ReconMatchType.Amb => "?",
        _ => "–"
    };

    public static string DeltaClass(decimal d) =>
        d == 0 ? "ok" : d < 0 ? "bad" : "warn";

    public static string DeltaText(decimal d) =>
        d > 0 ? $"+{d:0}" : d.ToString("0");
}
