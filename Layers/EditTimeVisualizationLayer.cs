using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace VSAtAGlance.Layers
{
    public class EditTimeVisualizationLayer : GazeResponseLayer
    {
        private EditTimeVisualizerModel _model;

        public override TimeSpan Timeout => TimeSpan.FromMilliseconds(750);

        public EditTimeVisualizationLayer(IWpfTextView editorInstance, SnapshotSpan span, EditTimeVisualizerModel model) : base(editorInstance, GazeAdornment.GAZE_VISUALIZATION_LAYER_NAME, span)
        {
            _model = model;
        }

        public override void Draw()
        {
            if (IsAdornmentVisible) Cleanup();

            EditorInstance.VisualElement.Dispatcher.Invoke(() =>
            {
                var adornment = EditTimeVisualizer.Create(EditorInstance, _model);
                var geometry = EditorInstance.TextViewLines.GetMarkerGeometry(Span);
                Canvas.SetLeft(adornment, geometry.Bounds.Left);
                Canvas.SetTop(adornment, geometry.Bounds.Top - geometry.Bounds.Height);

                PutLiveAdornment(Span, adornment);
            });
        }
    }
}
