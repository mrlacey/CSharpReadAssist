using System;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio;
using System.Threading.Tasks;
using System.Threading;

namespace CSharpReadAssist;

public class OutputPane
{
    private static Guid dsPaneGuid = new Guid("FA0BD86F-93CA-458E-B800-B191A453BC69");

    private static OutputPane instance;

    private readonly IVsOutputWindowPane pane;

    private OutputPane()
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        if (ServiceProvider.GlobalProvider.GetService(typeof(SVsOutputWindow)) is IVsOutputWindow outWindow
            && (ErrorHandler.Failed(outWindow.GetPane(ref dsPaneGuid, out this.pane)) || this.pane == null))
        {
            if (ErrorHandler.Failed(outWindow.CreatePane(ref dsPaneGuid, Vsix.Name, 1, 0)))
            {
                System.Diagnostics.Debug.WriteLine("Failed to create output pane.");
                return;
            }

            if (ErrorHandler.Failed(outWindow.GetPane(ref dsPaneGuid, out this.pane)) || (this.pane == null))
            {
                System.Diagnostics.Debug.WriteLine("Failed to get output pane.");
            }
        }
    }

    public static OutputPane Instance => instance ?? (instance = new OutputPane());

    public async Task ActivateAsync()
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(CancellationToken.None);

        this.pane?.Activate();
    }

    public async Task WriteAsync(string message)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(CancellationToken.None);

        this.pane?.OutputStringThreadSafe($"{message}{Environment.NewLine}");
    }
}