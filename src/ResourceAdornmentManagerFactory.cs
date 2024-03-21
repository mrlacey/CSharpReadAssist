using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace CSharpReadAssist;

[Export(typeof(IWpfTextViewCreationListener))]
[ContentType("CSharp"), ContentType("Razor"), ContentType("RazorCSharp"), ContentType("RazorCoreCSharp")]
[TextViewRole(PredefinedTextViewRoles.Document)]
internal sealed class ResourceAdornmentManagerFactory : IWpfTextViewCreationListener
{
    /// <summary>
    /// Defines the adornment layer for the adornment.
    /// </summary>
    [Export(typeof(AdornmentLayerDefinition))]
    [Name("CSharpReadAssistCommentLayer")]
    [Order(After = PredefinedAdornmentLayers.Selection, Before = PredefinedAdornmentLayers.Text)]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    public AdornmentLayerDefinition editorAdornmentLayer = null;

    /// <summary>
    /// Instantiates a ResourceAdornment manager when a textView is created.
    /// </summary>
    /// <param name="textView">The <see cref="IWpfTextView"/> upon which the adornment should be placed.</param>
    public void TextViewCreated(IWpfTextView textView)
    {
        textView.Properties.GetOrCreateSingletonProperty(() => new ResourceAdornmentManager(textView));
    }
}
