namespace ReconciliationApp.Frontend.State;

public sealed class LayoutState
{
    public bool IsSidebarOpen { get; private set; } = false;

    public event Action? OnChange;

    public void ToggleSidebar()
    {
        IsSidebarOpen = !IsSidebarOpen;
        Console.WriteLine($"[LayoutState] ToggleSidebar -> {(IsSidebarOpen ? "OPEN" : "CLOSED")}");
        OnChange?.Invoke();
    }

    public void CloseSidebar()
    {
        IsSidebarOpen = false;
        Console.WriteLine("[LayoutState] CloseSidebar -> CLOSED");
        OnChange?.Invoke();
    }

    public void OpenSidebar()
    {
        IsSidebarOpen = true;
        Console.WriteLine("[LayoutState] OpenSidebar -> OPEN");
        OnChange?.Invoke();
    }
}