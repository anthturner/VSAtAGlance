using System;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace VSAtAGlance.Layers
{
    public class TextHighlightInformationalGazeLayerElement : GazeResponseLayer
    {
        private double _weight;

        public override TimeSpan Timeout => TimeSpan.FromMilliseconds(100);

        public TextHighlightInformationalGazeLayerElement(IWpfTextView editorInstance, SnapshotSpan span, double weight): base(editorInstance, GazeAdornment.GAZE_INFORMATIONAL_LAYER_NAME, span)
        {
            _weight = weight;
        }

        public override void Draw()
        {
            if (IsAdornmentVisible) Cleanup();

            EditorInstance.VisualElement.Dispatcher.Invoke(() =>
            {
                var brush = new SolidColorBrush(
                                    Color.FromArgb((byte)((_weight / 1.0) * 255), 0, 255, 0)
                                );

                var adornment = new System.Windows.Shapes.Rectangle
                {
                    Fill = brush,
                    Stroke = System.Windows.Media.Brushes.Black,
                    Opacity = 0.6,
                    StrokeThickness = 1
                };

                // get the geometry around the span anchor to constrain the size to just the text area
                var geometry = EditorInstance.TextViewLines.GetMarkerGeometry(Span);
                Canvas.SetLeft(adornment, geometry.Bounds.Left);
                Canvas.SetTop(adornment, geometry.Bounds.Top);

                adornment.Width = geometry.Bounds.Width;
                adornment.Height = geometry.Bounds.Height;

                PutLiveAdornment(Span, adornment);
            });
        }
    }
}
