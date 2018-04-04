using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Text.Editor;
using System.Collections.Generic;
using VSAtAGlance.Targeting;

namespace VSAtAGlance
{
    internal interface IGazeTargeting
    {
        List<GazeTarget> GetTargetCandidateTokens(IWpfTextView editorInstance, Workspace workspace, double x, double y);
    }
}
