using System.Globalization;
using ReconciliationApp.Frontend.Contracts.Reconciliation;
using ReconciliationApp.Frontend.Models;

namespace ReconciliationApp.Frontend.State;

public sealed class ReconciliationStore
{
    private static readonly CultureInfo EsAr = CultureInfo.GetCultureInfo("es-AR");

    public event Action? OnChange;

    public ReconciliationView View { get; private set; } = ReconciliationView.Auto;
    public string Query { get; private set; } = "";
    public string ConfFilter { get; private set; } = "all"; // all | Alta | Alta+Media

    public HashSet<string> Selected { get; } = new();
    public string? SelectedKey { get; private set; }
    public bool DrawerOpen { get; private set; }

    public string RunId { get; private set; } = "";
    public string CompanyId { get; private set; } = "";
    public string CompanyName { get; private set; } = "";
    public string Period { get; private set; } = "";
    public string RunStatus { get; private set; } = "";

    public bool IsLoaded { get; private set; }
    public string? LoadError { get; private set; }

    public IReadOnlyList<ReconciliationRow> Rows => _rows;
    private readonly List<ReconciliationRow> _rows = [];

    public decimal DebtsAmtTotal { get; private set; }
    public decimal PaysAmtTotal { get; private set; }
    public int DebtsRowsTotal { get; private set; }
    public int PaysRowsTotal { get; private set; }

    public int AutoCount => _rows.Count(r => r.Status == RowStatus.Ok);
    public int PendingCount => _rows.Count(r => r.Status == RowStatus.Pending);
    public int ExceptionCount => _rows.Count(r => r.Status == RowStatus.Exception);
    public int AutoRate => _rows.Count == 0 ? 0 : (int)Math.Round((double)AutoCount / _rows.Count * 100);

    public bool CanConfirm => PendingCount == 0 && ExceptionCount == 0;

    public ReconciliationRow? SelectedRow => SelectedKey is null ? null : _rows.FirstOrDefault(r => r.Key == SelectedKey);

    public IEnumerable<ReconciliationRow> VisibleRows()
        => _rows.Where(PassesView).Where(PassesConf).Where(PassesQuery);

    public void LoadFromApi(ApiReconciliationRunDto dto)
    {
        _rows.Clear();

        RunId = dto.Summary.RunId;
        CompanyId = dto.Summary.CompanyId;
        CompanyName = dto.Summary.CompanyName;
        Period = dto.Summary.Period;
        RunStatus = dto.Summary.Status;

        DebtsRowsTotal = dto.Summary.DebtsRowsTotal;
        PaysRowsTotal = dto.Summary.PaymentsRowsTotal;
        DebtsAmtTotal = dto.Summary.DebtsAmountTotal;
        PaysAmtTotal = dto.Summary.PaymentsAmountTotal;

        foreach (var c in dto.Cases)
        {
            _rows.Add(new ReconciliationRow(
                CaseId: c.CaseId,
                DebtRowNumber: c.DebtRowNumber,
                PaymentRowNumber: c.PaymentRowNumber,
                Customer: c.Customer,
                DebtAmount: c.DebtAmount,
                PaymentAmount: c.PaymentAmount,
                Delta: c.Delta,
                Rule: c.Rule,
                Status: MapStatus(c.Status),
                Confidence: MapConfidence(c.Confidence),
                Type: MapMatchType(c.MatchType),
                Evidence: c.Evidence,
                Suggestion: c.Suggestion
            ));
        }

        IsLoaded = true;
        LoadError = null;
        Selected.Clear();
        SelectedKey = null;
        DrawerOpen = false;
        OnChange?.Invoke();
    }

    public void SetLoadError(string message)
    {
        LoadError = message;
        IsLoaded = false;
        OnChange?.Invoke();
    }

    public void SetView(ReconciliationView view)
    {
        View = view;
        Selected.Clear();
        SelectedKey = null;
        DrawerOpen = false;
        Query = "";
        if (View != ReconciliationView.Auto) ConfFilter = "all";
        OnChange?.Invoke();
    }

    public void SetQuery(string q)
    {
        Query = q ?? "";
        Selected.Clear();
        OnChange?.Invoke();
    }

    public void SetConfFilter(string conf)
    {
        ConfFilter = conf;
        Selected.Clear();
        OnChange?.Invoke();
    }

    public void ToggleSelected(string key, bool on)
    {
        if (on) Selected.Add(key);
        else Selected.Remove(key);
        OnChange?.Invoke();
    }

    public void SelectAllVisible(bool on)
    {
        var keys = VisibleRows().Select(r => r.Key).ToList();
        if (on) foreach (var k in keys) Selected.Add(k);
        else foreach (var k in keys) Selected.Remove(k);
        OnChange?.Invoke();
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
        OnChange?.Invoke();
    }

    public void CloseDrawer()
    {
        DrawerOpen = false;
        SelectedKey = null;
        OnChange?.Invoke();
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

        OnChange?.Invoke();
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

        OnChange?.Invoke();
    }

    public void AcceptSelected()
    {
        foreach (var k in Selected.ToList()) AcceptByKey(k);
        Selected.Clear();
        OnChange?.Invoke();
    }

    public void AcceptVisible()
    {
        foreach (var r in VisibleRows().ToList()) AcceptByKey(r.Key);
        Selected.Clear();
        OnChange?.Invoke();
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

    private static RowStatus MapStatus(string value)
        => value.ToLowerInvariant() switch
        {
            "ok" => RowStatus.Ok,
            "pending" => RowStatus.Pending,
            "exception" => RowStatus.Exception,
            _ => RowStatus.Pending
        };

    private static Confidence MapConfidence(string value)
        => value.ToLowerInvariant() switch
        {
            "high" => Confidence.Alta,
            "medium" => Confidence.Media,
            "low" => Confidence.Baja,
            _ => Confidence.Media
        };

    private static ReconMatchType MapMatchType(string value)
        => value.ToLowerInvariant() switch
        {
            "exact" => ReconMatchType.Exact,
            "partial" => ReconMatchType.Partial,
            "multi" => ReconMatchType.Multi,
            "duplicate" => ReconMatchType.Dup,
            "ambiguous" => ReconMatchType.Amb,
            "no_match" => ReconMatchType.NoMatch,
            _ => ReconMatchType.NoMatch
        };
}
