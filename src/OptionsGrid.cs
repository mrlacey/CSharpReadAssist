using System;
using System.ComponentModel;
using Microsoft.VisualStudio.Shell;

namespace CSharpReadAssist;

public class OptionsGrid : DialogPage
{
    [Category("Alignment")]
    [DisplayName("Bottom padding")]
    [Description("Pixels to add below the displayed value.")]
    public int BottomPadding { get; set; } = 0;

    [Category("Alignment")]
    [DisplayName("Top padding")]
    [Description("Pixels to add above the displayed value.")]
    public int TopPadding { get; set; } = 1;

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);

        // Settings page has been closed.
        // Prompt to reload resources in case of changes.
        Messenger.RequestReloadResources();
    }
}
