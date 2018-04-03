using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace VSAtAGlance.Targeting
{
    /// <summary>
    /// Implementation of a naive targeting system based on the use of Roslyn to walk the syntax tree and pick out likely investigative candidates
    /// </summary>
    public class RoslynInferenceGazeTargeting : IGazeTargeting
    {
        public List<SyntaxToken> GetTargetCandidateTokens(IWpfTextView editorInstance, double x, double y)
        {
            var potentialGazePoints = GetPointsFromGazeCenter(editorInstance, x, y, strideWidthX: editorInstance.LineHeight / 2, strideWidthY: editorInstance.LineHeight);
            var gazedElements = FindGazedElementsFromSnapshotPoints(editorInstance, potentialGazePoints);
            return gazedElements;
        }

        private List<SnapshotPoint> GetPointsFromGazeCenter(IWpfTextView editorInstance, double x, double y, int radius = 50, double strideWidthX = 8, double strideWidthY = 10)
        {
            var left = x - radius;
            var top = y - radius;
            if (left < 0) left = 0;
            if (top < 0) top = 0;

            var pts = new List<SnapshotPoint>();
            for (double i = left; i < x + radius; i += strideWidthX)
                for (double j = top; j < y + radius; j += strideWidthY)
                {
                    var snapshot = GetSnapshotPointFromCoordinates(editorInstance, i, j);
                    if (snapshot.HasValue)
                    {
                        if (pts.Any(p => p.Position == snapshot.Value.Position))
                            continue;
                        pts.Add(snapshot.Value);
                    }
                }
            return pts;
        }

        private SnapshotPoint? GetSnapshotPointFromCoordinates(IWpfTextView editorInstance, double x, double y)
        {
            var dte = Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
            var line = editorInstance.TextViewLines.GetTextViewLineContainingYCoordinate(y);
            if (line == null)
                return null;
            var bufferPosition = line.GetBufferPositionFromXCoordinate(x);
            if (!bufferPosition.HasValue)
                return null;

            return bufferPosition;
        }

        private List<SyntaxToken> FindGazedElementsFromSnapshotPoints(IWpfTextView editorInstance, List<SnapshotPoint> points)
        {
            var tokens = new List<SyntaxToken>();

            // extract the roslyn syntaxnode from the gazed point
            Document document = editorInstance.TextBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            SemanticModel semanticModel = document.GetSemanticModelAsync().Result;
            if (semanticModel == null)
                return tokens;

            foreach (var point in points)
            {
                var result = document.GetSyntaxRootAsync().Result.FindToken(point);
                if (result != null && !tokens.Contains(result))
                    tokens.Add(result);
            }
            return tokens;
        }
    }
}
