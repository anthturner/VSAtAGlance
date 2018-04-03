using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace VSAtAGlance.Layers
{
    public class EditTimeVisualizationLayer : GazeResponseLayer
    {
        private UIElement _adornment;

        public override TimeSpan Timeout => TimeSpan.FromMilliseconds(750);

        public EditTimeVisualizationLayer(IWpfTextView editorInstance, SnapshotSpan span, EditTimeVisualizerModel model) : base(editorInstance, GazeAdornment.GAZE_VISUALIZATION_LAYER_NAME, span)
        {
            _adornment = editorInstance.VisualElement.Dispatcher.Invoke(() => EditTimeVisualizer.Create(editorInstance, (EditTimeVisualizerModel)model));
        }

        public override void Draw()
        {
            EditorInstance.VisualElement.Dispatcher.Invoke(() =>
            {
                var geometry = EditorInstance.TextViewLines.GetMarkerGeometry(Span);
                Canvas.SetLeft(_adornment, geometry.Bounds.Left);
                Canvas.SetTop(_adornment, geometry.Bounds.Top - geometry.Bounds.Height);

                PutLiveAdornment(Span, _adornment);
            });
        }
    }
}
