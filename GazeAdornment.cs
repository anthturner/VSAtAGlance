using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.Text;
using System;
using System.Linq;
using System.Windows.Media;
using VSAtAGlance.EyeTracking;
using VSAtAGlance.Layers;
using VSAtAGlance.Targeting;

namespace VSAtAGlance
{
    public sealed class GazeAdornment
    {
        public const string GAZE_VISUALIZATION_LAYER_NAME = "VSAtAGlanceGazeVisualizationLayer";
        public const string GAZE_INFORMATIONAL_LAYER_NAME = "VSAtAGlanceGazeInformationalLayer";

        private Microsoft.CodeAnalysis.Workspace Workspace;
        private readonly Microsoft.VisualStudio.Text.Editor.IWpfTextView View;
        private GazeLayerManager LayerManager { get; set; }
        private IGazeTargeting GazeTargeting { get; set; } = new RoslynInferenceGazeTargeting();
        private IEyeTrackingProvider EyeTracking { get; set; } = new SimulatedEyeTrackingProvider();
        private CoordinateStabilizer Stabilization { get; set; } = new CoordinateStabilizer();

        bool _inFlight = false;

        public GazeAdornment(Microsoft.VisualStudio.Text.Editor.IWpfTextView view)
        {
            var componentModel = (Microsoft.VisualStudio.ComponentModelHost.IComponentModel)Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(Microsoft.VisualStudio.ComponentModelHost.SComponentModel));
            Workspace = componentModel.GetService<Microsoft.VisualStudio.LanguageServices.VisualStudioWorkspace>();
            View = view;

            LayerManager = GazeLayerManager.Get(view);
            LayerManager.SingletonLayers = new System.Collections.Generic.List<GazeResponseLayer>()
            {
                new EllipseInformationalGazeLayerElement(View, Brushes.Red, 5, 5), // show a 5x5 core target that is the discrete X,Y coordinates
                new EllipseInformationalGazeLayerElement(View, Brushes.Cyan, 100, 100), // show a 100x100 extended target that is the scan area for the gaze targeting system
            };

            EyeTracking.PointAvailable += (sender, e) => Stabilization.AddPoint(e);
            Stabilization.StabilizedPointAvailable += (sender, e) =>
            {
                if (_inFlight)
                    return;

                _inFlight = true;

                // workflow goes here, point should be stabilized coming from point provider
                try
                {
                    if (View.VisualElement.Dispatcher.Invoke(() => (View.VisualElement.ActualHeight == 0 || !View.VisualElement.IsInitialized || !View.VisualElement.IsLoaded)))
                    {
                        _inFlight = false;
                        return; // gaze point available before WPF draw is finished
                    }

                    // localize point to our editor view
                    var localPt = View.VisualElement.Dispatcher.Invoke(() => View.VisualElement.PointFromScreen(e));
                    localPt.X += View.ViewportLeft;
                    localPt.Y += View.ViewportTop;
                    
                    // infer/guess gaze targets
                    var gazedElements = GazeTargeting.GetTargetCandidateTokens(View, Workspace, localPt.X, localPt.Y);

                    // draw singleton layers (coordinate-based)
                    LayerManager.DrawSingletonLayers(localPt.X, localPt.Y);

                    // draw highlights around gazed elements
                    LayerManager.ScrubLayers();
                    foreach (GazeTarget element in gazedElements.OrderByDescending(el => el.Weight))
                    {
                        var span = new SnapshotSpan(View.TextSnapshot, element.DefinitionLocation.Start, element.DefinitionLocation.Length);
                        if (!LayerManager.IsTracking(span))
                        {
                            // gradate opacity of the debug layer based on target weights (on green)
                            LayerManager.Layers.Add(new TextHighlightInformationalGazeLayerElement(View, span, element.Weight));
                        }

                        var model = EditTimeVisualizerModel.Create(element);
                        if (model != null && model.IsValid)
                            LayerManager.Layers.Add(new EditTimeVisualizationLayer(View, span, model));
                    }
                    LayerManager.Draw();
                }
                catch (Exception ex)
                {

                }

                _inFlight = false;
            };
        }
    }

    namespace MefRegistration
    {
        using System.ComponentModel.Composition;
        using Microsoft.VisualStudio.Utilities;
        using Microsoft.VisualStudio.Text.Editor;

        [Export(typeof(IWpfTextViewCreationListener)), ContentType("text"), TextViewRole(PredefinedTextViewRoles.Document)]
        public sealed class GazeAdornmentTextViewCreationListener : IWpfTextViewCreationListener
        {
            // Fired on *creation* of an editor tab instance, not on refocus/rerender
            public void TextViewCreated(IWpfTextView textView) => new GazeAdornment(textView);

#pragma warning disable CS0169 // don't complain about unused members here, since they're bound by MEF -- compiler can't infer this
            [Export(typeof(AdornmentLayerDefinition)), Name(GazeAdornment.GAZE_VISUALIZATION_LAYER_NAME), Order(After = PredefinedAdornmentLayers.Text)]
            private AdornmentLayerDefinition gazeAdornmentOverlayLayer;

            [Export(typeof(AdornmentLayerDefinition)), Name(GazeAdornment.GAZE_INFORMATIONAL_LAYER_NAME), Order(Before = PredefinedAdornmentLayers.Text)]
            private AdornmentLayerDefinition gazeDebugAdornmentLayer;
#pragma warning restore CS0169
        }
    }
}
