using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using System;
using System.Linq;
using System.Windows.Controls;
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

        private readonly Microsoft.VisualStudio.Text.Editor.IWpfTextView View;
        private Microsoft.CodeAnalysis.Workspace Workspace;
        private UserControl Adornment;

        private ViewState viewportState;
        private GazeLayerManager LayerManager { get; set; }
        private IGazeTargeting GazeTargeting { get; set; }
        private IEyeTrackingProvider EyeTracking { get; set; }

        bool _inFlight = false;

        public GazeAdornment(Microsoft.VisualStudio.Text.Editor.IWpfTextView view)
        {
            var componentModel = (Microsoft.VisualStudio.ComponentModelHost.IComponentModel)Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(Microsoft.VisualStudio.ComponentModelHost.SComponentModel));
            Workspace = componentModel.GetService<Microsoft.VisualStudio.LanguageServices.VisualStudioWorkspace>();

            View = view;

            GazeTargeting = new RoslynInferenceGazeTargeting();
            EyeTracking = new TobiiEyeTrackingProvider();
            LayerManager = GazeLayerManager.Get(view);
            LayerManager.SingletonLayers = new System.Collections.Generic.List<GazeResponseLayer>()
            {
                new EllipseInformationalGazeLayerElement(View, Brushes.Red, 5, 5),
                new EllipseInformationalGazeLayerElement(View, Brushes.Cyan, 100, 100),
            };

            EyeTracking.PointAvailable += (sender, e) =>
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
                    var gazedElements = GazeTargeting.GetTargetCandidateTokens(View, localPt.X, localPt.Y);
                    gazedElements = gazedElements.Where(ge => 
                        ge.Parent is IdentifierNameSyntax ||
                        ge.Parent is ParameterSyntax
                    ).ToList();

                    // draw singleton layers (coordinate-based)
                    LayerManager.DrawSingletonLayers(localPt.X, localPt.Y);

                    // draw highlights around gazed elements
                    LayerManager.ScrubLayers();
                    foreach (var element in gazedElements)
                    {
                        var span = new SnapshotSpan(View.TextSnapshot, element.FullSpan.Start, element.FullSpan.Length);
                        if (!LayerManager.IsTracking(span))
                        {
                            LayerManager.Layers.Add(new TextHighlightInformationalGazeLayerElement(View, span, Brushes.Green));

                            var model = EditTimeVisualizerModel.Create(element.Parent);
                            if (model != null && model.IsValid)
                                LayerManager.Layers.Add(new EditTimeVisualizationLayer(View, span, model));
                        }
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
            // This class will be instantiated the first time a text document is opened. (That's because our VSIX manifest
            // lists this project, i.e. this DLL, and VS scans all such listed DLLs to find all types with the right attributes).
            // The TextViewCreated event will be raised each time a text document tab is created. It won't be
            // raised for subsequent re-activation of an existing document tab.
            public void TextViewCreated(IWpfTextView textView) => new GazeAdornment(textView);

#pragma warning disable CS0169 // C# warning "the field editorAdornmentLayer is never used" -- but it is used, by MEF!
            [Export(typeof(AdornmentLayerDefinition)), Name(GazeAdornment.GAZE_VISUALIZATION_LAYER_NAME), Order(After = PredefinedAdornmentLayers.Text)]
            private AdornmentLayerDefinition gazeAdornmentOverlayLayer;

            [Export(typeof(AdornmentLayerDefinition)), Name(GazeAdornment.GAZE_INFORMATIONAL_LAYER_NAME), Order(Before = PredefinedAdornmentLayers.Text)]
            private AdornmentLayerDefinition gazeDebugAdornmentLayer;
#pragma warning restore CS0169
        }
    }
}
