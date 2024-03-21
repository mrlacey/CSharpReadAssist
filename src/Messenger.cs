namespace CSharpReadAssist;

public static class Messenger
{
    public delegate void ReloadResourcesEventHandler();

    public static event ReloadResourcesEventHandler ReloadResources;

    public static void RequestReloadResources()
    {
        System.Diagnostics.Debug.WriteLine("RequestReloadResources");
        ReloadResources?.Invoke();
    }
}