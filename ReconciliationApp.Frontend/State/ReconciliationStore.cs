using System.Globalization;
using ReconciliationApp.Frontend.Models;

namespace ReconciliationApp.Frontend.State;

public sealed class ReconciliationStore
{
    public event Action? OnChange;

    private void Notify()
    {
        OnChange?.Invoke();
    }

    private static readonly CultureInfo EsAr = CultureInfo.GetCultureInfo("es-AR");

    public ReconciliationView View { get; private set; } = ReconciliationView.Auto;
    public string Query { get; private set; } = "";
    public string ConfFilter { get; private set; } = "all"; // all | Alta | Alta+Media

    public HashSet<string> Selected { get; } = new();
    public string? SelectedKey { get; private set; }
    public bool DrawerOpen { get; private set; }

    public IReadOnlyList<ReconciliationRow> Rows => _rows;
    private readonly List<ReconciliationRow> _rows = new()
    {
        new(1,1,"C1",1000,1000,0,"Cliente+Monto",RowStatus.Ok,Confidence.Alta,ReconMatchType.Exact,"Mismo cliente · monto exacto · ref coincide","Aceptar"),
        new(4,3,"C3",1200,1200,0,"Cliente+Monto",RowStatus.Ok,Confidence.Alta,ReconMatchType.Exact,"Mismo cliente · monto exacto","Aceptar"),
        new(5,4,"C4",700,700,0,"Cliente+Monto",RowStatus.Ok,Confidence.Alta,ReconMatchType.Exact,"Mismo cliente · monto exacto","Aceptar"),
        new(7,6,"C5",350,350,0,"Cliente+Monto",RowStatus.Ok,Confidence.Media,ReconMatchType.Exact,"Cliente coincide · sin ref","Aceptar (c/ cuidado)"),
        new(2,2,"C1",500,450,-50,"Monto cercano",RowStatus.Pending,Confidence.Media,ReconMatchType.Partial,"Pago menor · posible parcial","Revisar parcial"),
        new(6,8,"C2",900,930,30,"Monto cercano",RowStatus.Pending,Confidence.Media,ReconMatchType.Amb,"Varias deudas posibles","Revisar"),
        new(3,5,"C2",200,200,0,"Duplicado?",RowStatus.Exception,Confidence.Baja,ReconMatchType.Dup,"Pago similar ya conciliado","Excepción"),
    };

    public decimal DebtsAmtTotal => 1_250_000m;
    public decimal PaysAmtTotal => 1_240_000m;
    public int DebtsRowsTotal => 28;
    public int PaysRowsTotal => 26;

    public int AutoCount => _rows.Count(r => r.Status == RowStatus.Ok);
    public int PendingCount => _rows.Count(r => r.Status == RowStatus.Pending);
    public int ExceptionCount => _rows.Count(r => r.Status == RowStatus.Exception);
    public int AutoRate => _rows.Count == 0 ? 0 : (int)Math.Round((double)AutoCount / _rows.Count * 100);

    public bool CanConfirm => PendingCount == 0 && ExceptionCount == 0;

    public ReconciliationRow? SelectedRow => SelectedKey is null ? null : _rows.FirstOrDefault(r => r.Key == SelectedKey);

    public IEnumerable<ReconciliationRow> VisibleRows()
        => _rows.Where(PassesView).Where(PassesConf).Where(PassesQuery);

    public void SetView(ReconciliationView view)
    {
        View = view;
        Selected.Clear();
        SelectedKey = null;
        DrawerOpen = false;
        Query = "";
        if (View != ReconciliationView.Auto) ConfFilter = "all";

        Notify();
    }

    public void SetQuery(string q)
    {
        Query = q ?? "";
        Selected.Clear();

        Notify();
    }

    public void SetConfFilter(string conf)
    {
        ConfFilter = conf;
        Selected.Clear();

        Notify();
    }

    public void ToggleSelected(string key, bool on)
    {
        if (on) Selected.Add(key);
        else Selected.Remove(key);

        Notify();
    }

    public void SelectAllVisible(bool on)
    {
        var keys = VisibleRows().Select(r => r.Key).ToList();
        if (on)
            foreach (var k in keys) Selected.Add(k);
        else
            foreach (var k in keys) Selected.Remove(k);

        Notify();
    }

    public bool AllVisibleSelected()
    {
        var keys = VisibleRows().Select(r => r.Key).ToList();
        return keys.Count > 0 && keys.All(k => Selected.Contains(k));
    }

    public bool SomeVisibleSelected()
    {
        var keys = VisibleRows().Select(r => r.Key).ToList();
        var count = keys.Count(k => Selected.Contains(k));
        return count > 0 && count < keys.Count;
    }

    public void OpenDrawer(string key)
    {
        SelectedKey = key;
        DrawerOpen = true;

        Notify();
    }

    public void CloseDrawer()
    {
        DrawerOpen = false;
        SelectedKey = null;

        Notify();
    }

    public void AcceptByKey(string key)
    {
        var idx = _rows.FindIndex(r => r.Key == key);
        if (idx < 0) return;

        var r = _rows[idx];
        var conf = r.Confidence == Confidence.Baja ? Confidence.Media : r.Confidence;

        _rows[idx] = r with
        {
            Status = RowStatus.Ok,
            Confidence = conf,
            Suggestion = "Aceptar"
        };

        Notify();
    }

    public void ExceptionByKey(string key)
    {
        var idx = _rows.FindIndex(r => r.Key == key);
        if (idx < 0) return;

        var r = _rows[idx];
        _rows[idx] = r with
        {
            Status = RowStatus.Exception,
            Suggestion = "Excepción"
        };

        Notify();
    }

    public void AcceptSelected()
    {
        foreach (var k in Selected.ToList()) AcceptByKey(k);
        Selected.Clear();

        Notify();
    }

    public void AcceptVisible()
    {
        foreach (var r in VisibleRows().ToList()) AcceptByKey(r.Key);
        Selected.Clear();

        Notify();
    }

    public static string Money(decimal n)
        => n.ToString("C0", EsAr);

    private bool PassesView(ReconciliationRow r)
        => View switch
        {
            ReconciliationView.Auto => r.Status == RowStatus.Ok,
            ReconciliationView.Pending => r.Status == RowStatus.Pending,
            _ => r.Status == RowStatus.Exception
        };

    private bool PassesConf(ReconciliationRow r)
    {
        if (View != ReconciliationView.Auto) return true;
        return ConfFilter switch
        {
            "Alta" => r.Confidence == Confidence.Alta,
            "Alta+Media" => r.Confidence is Confidence.Alta or Confidence.Media,
            _ => true
        };
    }

    private bool PassesQuery(ReconciliationRow r)
    {
        var q = (Query ?? "").Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(q)) return true;

        var blob = $"{r.DebtRowNumber} {r.PaymentRowNumber} {r.Customer} {r.DebtAmount} {r.PaymentAmount} {r.Delta} {r.Rule} {r.Status} {r.Confidence} {r.Type} {r.Evidence} {r.Suggestion}"
            .ToLowerInvariant();

        return blob.Contains(q);
    }
}
