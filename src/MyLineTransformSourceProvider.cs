using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Utilities;

namespace CSharpReadAssist;

[Export(typeof(ILineTransformSourceProvider))]
[ContentType("CSharp"), ContentType("Razor"), ContentType("RazorCSharp"), ContentType("RazorCoreCSharp")]
[TextViewRole(PredefinedTextViewRoles.Document)]
internal class MyLineTransformSourceProvider : ILineTransformSourceProvider
{
    ILineTransformSource ILineTransformSourceProvider.Create(IWpfTextView view)
    {
        ResourceAdornmentManager manager = view.Properties.GetOrCreateSingletonProperty<ResourceAdornmentManager>(() => new ResourceAdornmentManager(view));
        return new MyLineTransformSource(manager);
    }
}
