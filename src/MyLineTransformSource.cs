﻿using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;

namespace CSharpReadAssist;

internal class MyLineTransformSource : ILineTransformSource
{
    private readonly ResourceAdornmentManager manager;

    public MyLineTransformSource(ResourceAdornmentManager manager)
    {
        this.manager = manager;
    }

    LineTransform ILineTransformSource.GetLineTransform(ITextViewLine line, double yPosition, ViewRelativePosition placement)
    {
        int lineNumber = line.Snapshot.GetLineFromPosition(line.Start.Position).LineNumber;
        LineTransform lineTransform;

        if (this.manager.DisplayedTextBlocks.ContainsKey(lineNumber)
         && this.manager.DisplayedTextBlocks[lineNumber].Count > 0)
        {
            var spaceAboveLine = line.DefaultLineTransform.TopSpace + ((CSharpReadAssistPackage.Instance.Options.TopPadding + CSharpReadAssistPackage.Instance.Options.BottomPadding));
            var spaceBelowLine = line.DefaultLineTransform.BottomSpace;
            lineTransform = new LineTransform(spaceAboveLine + ResourceAdornmentManager.TextSize, spaceBelowLine, 1.0);
        }
        else
        {
            lineTransform = new LineTransform(0, 0, 1.0);
        }

        return lineTransform;
    }
}