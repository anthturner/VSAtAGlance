using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Text.Editor;
using System.Collections.Generic;

namespace VSAtAGlance
{
    internal interface IGazeTargeting
    {
        List<SyntaxToken> GetTargetCandidateTokens(IWpfTextView editorInstance, double x, double y);
    }
}
