using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Threading;

namespace CSharpReadAssist;

public class ResourceAdornmentManager : IDisposable
{
    private readonly IAdornmentLayer layer;
    private readonly IWpfTextView view;
    private readonly List<(string alias, int lineNo, string resName)> aliases = new();
    private bool hasDoneInitialCreateVisualsPass = false;

    public ResourceAdornmentManager(IWpfTextView view)
    {
        this.view = view;
        this.layer = view.GetAdornmentLayer("CSharpReadAssistCommentLayer");

        ThreadHelper.ThrowIfNotOnUIThread();

        if (CSharpReadAssistPackage.Instance == null)
        {
            // Try and force load the project if it hasn't already loaded
            // so can access the configured options.
            if (ServiceProvider.GlobalProvider.GetService(typeof(SVsShell)) is IVsShell shell)
            {
                // IVsPackage package = null;
                Guid packageToBeLoadedGuid = new(CSharpReadAssistPackage.PackageGuidString);
                shell.LoadPackage(ref packageToBeLoadedGuid, out _);
            }
        }

        this.view.LayoutChanged += this.LayoutChangedHandler;
    }

    // Initialize to the same default as VS
    public static uint TextSize { get; set; } = 10;

    // Initialize to a reasonable value for display on light or dark themes/background.
    public static Color TextForegroundColor { get; set; } = Colors.Gray;

    // Keep a record of displayed text blocks so we can remove them as soon as changed or no longer appropriate
    // Also use this to identify lines to pad so the textblocks can be seen
    public Dictionary<int, List<(TextBlock textBlock, string resName)>> DisplayedTextBlocks { get; set; } = new Dictionary<int, List<(TextBlock textBlock, string resName)>>();

    /// <summary>
    /// This is called by the TextView when closing. Events are unsubscribed here.
    /// </summary>
    /// <remarks>
    /// It's actually called twice - once by the IPropertyOwner instance, and again by the ITagger instance.
    /// </remarks>
    public void Dispose() => this.UnsubscribeFromViewerEvents();

    /// <summary>
    /// On layout change add the adornment to any reformatted lines.
    /// </summary>
#pragma warning disable VSTHRD100 // Avoid async void methods
    private async void LayoutChangedHandler(object sender, TextViewLayoutChangedEventArgs e)
#pragma warning restore VSTHRD100 // Avoid async void methods
    {
        var collection = this.hasDoneInitialCreateVisualsPass ? (IEnumerable<ITextViewLine>)e.NewOrReformattedLines : this.view.TextViewLines;

        foreach (ITextViewLine line in collection)
        {
            int lineNumber = line.Snapshot.GetLineFromPosition(line.Start.Position).LineNumber;

            try
            {
                await this.CreateVisualsAsync(line, lineNumber);
            }
            catch (InvalidOperationException ex)
            {
                await OutputPane.Instance?.WriteAsync("Error handling layout changed");
                await OutputPane.Instance?.WriteAsync(ex.Message);
                await OutputPane.Instance?.WriteAsync(ex.Source);
                await OutputPane.Instance?.WriteAsync(ex.StackTrace);
            }

            this.hasDoneInitialCreateVisualsPass = true;
        }
    }

    /// <summary>
    /// Scans text line for use of resource class, then adds new adornment.
    /// </summary>
    private async Task CreateVisualsAsync(ITextViewLine line, int lineNumber)
    {
        // TODO: Cache text retrieved from the resource file based on fileName and key. - Invalidate the cache when reload resource files. This will save querying the XMLDocument each time.
        try
        {
            string lineText = line.Extent.GetText();

            // The extent will include all of a collapsed section
            if (lineText.Contains(Environment.NewLine))
            {
                // We only want the first "line" here as that's all that can be seen on screen
                lineText = lineText.Substring(0, lineText.IndexOf(Environment.NewLine, StringComparison.InvariantCultureIgnoreCase));
            }

            // Remove any textblocks displayed on this line so it won't conflict with anything we add below.
            // Handles no textblocks to show or the text to display having changed.
            if (this.DisplayedTextBlocks.ContainsKey(lineNumber))
            {
                foreach (var (textBlock, _) in this.DisplayedTextBlocks[lineNumber])
                {
                    this.layer.RemoveAdornment(textBlock);
                }

                this.DisplayedTextBlocks.Remove(lineNumber);
            }

            var lineAdornments = await GetSubstringsToAdorn(lineText);

            if (lineAdornments.Any())
            {
                var lastLeft = double.NaN;

                var sw = new System.Diagnostics.Stopwatch();
                sw.Start();

                // Go through them right-to-left (highest to lowest index) so know if there's anything that might overlap
                foreach (var item in lineAdornments.OrderByDescending(l => l.Index))
                {
                    string displayText = item.DisplayText;

                    if (string.IsNullOrWhiteSpace(displayText))
                    {
                        break;
                    }

                    if (!this.DisplayedTextBlocks.ContainsKey(lineNumber))
                    {
                        this.DisplayedTextBlocks.Add(lineNumber, new List<(TextBlock textBlock, string resName)>());
                    }

                    if (!string.IsNullOrWhiteSpace(displayText) && TextSize > 0)
                    {
                        var brush = new SolidColorBrush(TextForegroundColor);
                        brush.Freeze();

                        // Add 1 for bolding
                        var height = TextSize + 1 + CSharpReadAssistPackage.Instance?.Options.TopPadding ?? 0 + CSharpReadAssistPackage.Instance?.Options.BottomPadding ?? 0;

                        var tb = new TextBlock
                        {
                            Foreground = brush,
                            Text = displayText,
                            FontSize = TextSize,
                            FontWeight = FontWeights.SemiBold,
                            Height = height,
                            VerticalAlignment = VerticalAlignment.Top,
                            Padding = new Thickness(0, CSharpReadAssistPackage.Instance?.Options.TopPadding ?? 0, 0, 0),
                        };

                        this.DisplayedTextBlocks[lineNumber].Add((tb, displayText));

                        // Get coordinates of text
                        int start = line.Extent.Start.Position + item.Index;
                        int end = line.Start + (line.Extent.Length - 1);
                        var span = new SnapshotSpan(this.view.TextSnapshot, Span.FromBounds(start, end));
                        var lineGeometry = this.view.TextViewLines.GetMarkerGeometry(span);

                        if (!double.IsNaN(lastLeft))
                        {
                            tb.MaxWidth = lastLeft - lineGeometry.Bounds.Left - 5; // Minus 5 for padding
                            tb.TextTrimming = TextTrimming.CharacterEllipsis;
                        }

                        Canvas.SetLeft(tb, lineGeometry.Bounds.Left);
                        Canvas.SetTop(tb, line.TextTop - tb.Height);

                        lastLeft = lineGeometry.Bounds.Left;

                        this.layer.AddAdornment(AdornmentPositioningBehavior.TextRelative, line.Extent, tag: null, adornment: tb, removedCallback: null);
                    }
                }

                sw.Stop();
                if (sw.Elapsed > TimeSpan.FromMilliseconds(100))
                {
                    await OutputPane.Instance.WriteAsync($"Getting text to display took longer than expected: {sw.ElapsedMilliseconds} milliseconds");
                }
            }
        }
        catch (Exception ex)
        {
            await OutputPane.Instance?.WriteAsync("Error creating visuals");
            await OutputPane.Instance?.WriteAsync(ex.Message);
            await OutputPane.Instance?.WriteAsync(ex.Source);
            await OutputPane.Instance?.WriteAsync(ex.StackTrace);
        }
    }

    public static async Task<List<(int Index, string DisplayText)>> GetSubstringsToAdorn(string source)
    {
        var result = new List<(int, string)>();

        try
        {
            var andIndex = source.IndexOf("&& ");

            if (andIndex > -1 && char.IsWhiteSpace(source[andIndex - 1]))
            {
                result.Add((andIndex, "AND"));
            }

            var orIndex = source.IndexOf("|| ");

            if (orIndex > -1 && char.IsWhiteSpace(source[orIndex - 1]))
            {
                result.Add((orIndex, "OR"));
            }

            var notIndex = source.IndexOf("!");

            if (notIndex > -1 && source[notIndex - 1] == '(' && char.IsLetterOrDigit(source[notIndex + 1]))
            {
                result.Add((notIndex, "NOT"));
            }
        }
        catch (Exception ex)
        {
            await OutputPane.Instance?.WriteAsync("Error in GetSubstringsToAdorn");
            await OutputPane.Instance?.WriteAsync(source);
            await OutputPane.Instance?.WriteAsync(ex.Message);
            await OutputPane.Instance?.WriteAsync(ex.Source);
            await OutputPane.Instance?.WriteAsync(ex.StackTrace);
        }

        return result;
    }

    private void UnsubscribeFromViewerEvents()
    {
        this.view.LayoutChanged -= this.LayoutChangedHandler;
    }
}
