using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
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
        public List<GazeTarget> GetTargetCandidateTokens(IWpfTextView editorInstance, Workspace workspace, double x, double y)
        {
            var potentialGazePoints = GetPointsFromGazeCenter(editorInstance, x, y, strideWidthX: editorInstance.LineHeight / 2, strideWidthY: editorInstance.LineHeight);
            return ScoreLikelyGazeTargetsFromScatteredPoints(editorInstance, workspace, potentialGazePoints);
        }

        private List<SnapshotPoint> GetPointsFromGazeCenter(IWpfTextView editorInstance, double x, double y, int radius = 50, double strideWidthX = 8, double strideWidthY = 10)
        {
            var left = x - radius;
            var top = y - radius;
            if (left < 0) left = 0;
            if (top < 0) top = 0;

            var pts = new List<SnapshotPoint>();
            for (double i = left; i < left + (radius * 2); i += strideWidthX)
                for (double j = top; j < top + (radius * 2); j += strideWidthY)
                {
                    var line = editorInstance.TextViewLines.GetTextViewLineContainingYCoordinate(j);
                    if (line == null)
                        continue;

                    var bufferPosition = line.GetBufferPositionFromXCoordinate(i);
                    if (!bufferPosition.HasValue)
                        continue;

                    if (pts.Any(p => p.Position == bufferPosition.Value.Position))
                        continue;
                    pts.Add(bufferPosition.Value);
                }
            return pts;
        }

        private List<GazeTarget> ScoreLikelyGazeTargetsFromScatteredPoints(IWpfTextView editorInstance, Workspace workspace, List<SnapshotPoint> points)
        {
            Document document = editorInstance.TextBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            SemanticModel semanticModel = document.GetSemanticModelAsync().Result;

            var foundTokens = new List<SyntaxToken>();
            var possibleTargets = new List<GazeTarget>();
            foreach (var point in points)
            {
                var symbolsInPosition = semanticModel.LookupSymbols(point.Position).Where(s =>
                    s.Kind == SymbolKind.Field ||
                    s.Kind == SymbolKind.Local ||
                    s.Kind == SymbolKind.Parameter ||
                    s.Kind == SymbolKind.Property ||
                    s.Kind == SymbolKind.RangeVariable);
                foreach (var symbol in symbolsInPosition)
                {
                    // todo: weight results by a scoring metric... vector distance from centroid times a coefficient for SymbolKind?
                    // todo: route async/TPL throughout
                    
                    var symbolReferences = SymbolFinder.FindReferencesAsync(symbol, workspace.CurrentSolution).Result;
                    var target = new GazeTarget() { DataModel = symbol };
                    var token = document.GetSyntaxRootAsync().Result.FindToken(point);
                    target.DefinitionLocation = new GazeTargetLocation(token.Span.Start, token.Span.Length);

                    foreach (var symbolReference in symbolReferences)
                        foreach (var location in symbolReference.Locations)
                            target.GazedLocations.Add(new GazeTargetLocation(location.Location.SourceSpan.Start, location.Location.SourceSpan.Length));
                    possibleTargets.Add(target);
                }
            }
            return possibleTargets;
        }
    }
}
