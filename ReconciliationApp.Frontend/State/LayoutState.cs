namespace ReconciliationApp.Frontend.State;

public sealed class LayoutState
{
    public bool IsSidebarOpen { get; private set; } = false;

    public event Action? OnChange;

    public void ToggleSidebar()
    {
        IsSidebarOpen = !IsSidebarOpen;
        OnChange?.Invoke();
    }

    public void CloseSidebar()
    {
        IsSidebarOpen = false;
        OnChange?.Invoke();
    }

    public void OpenSidebar()
    {
        IsSidebarOpen = true;
        OnChange?.Invoke();
    }
}
