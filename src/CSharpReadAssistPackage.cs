using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio.Shell;
using static Microsoft.VisualStudio.VSConstants;
using Task = System.Threading.Tasks.Task;

namespace CSharpReadAssist;

[ProvideAutoLoad(UICONTEXT.CSharpProject_string, PackageAutoLoadFlags.BackgroundLoad)]
[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
[InstalledProductRegistration(Vsix.Name, Vsix.Description, Vsix.Version)] // Info on this package for Help/About
[ProvideOptionPage(typeof(OptionsGrid), Vsix.Name, "General", 0, 0, true)]
[Guid(CSharpReadAssistPackage.PackageGuidString)]
public sealed class CSharpReadAssistPackage : AsyncPackage
{
    public const string PackageGuidString = "f805b301-32f7-415d-b551-6fc4198349ff";

    public static CSharpReadAssistPackage Instance { get; private set; }

    public OptionsGrid Options
    {
        get
        {
            return (OptionsGrid)this.GetDialogPage(typeof(OptionsGrid));
        }
    }

    protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
    {
        await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        // TODO: Review what (if anything) need to load or set up.

        Instance = this;
    }
}
