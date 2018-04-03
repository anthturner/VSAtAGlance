using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Editor;

namespace VSAtAGlance.Layers
{
    public class EllipseInformationalGazeLayerElement : GazeResponseLayer
    {
        private Brush _color;
        private int _width, _height;

        public EllipseInformationalGazeLayerElement(IWpfTextView editorInstance, Brush color, int width, int height) : base(editorInstance, GazeAdornment.GAZE_INFORMATIONAL_LAYER_NAME)
        {
            _color = color;
            _width = width;
            _height = height;
        }

        public override void Draw(double x, double y)
        {
            if (IsAdornmentVisible) Cleanup();

            EditorInstance.VisualElement.Dispatcher.Invoke(() =>
            {
                var adornment = new System.Windows.Shapes.Ellipse
                {
                    Width = _width,
                    Height = _height,
                    Fill = _color,
                    Stroke = System.Windows.Media.Brushes.Black,
                    Opacity = 0.6,
                    StrokeThickness = 1
                };

                Canvas.SetLeft(adornment, x - adornment.Width / 2);
                Canvas.SetTop(adornment, y - adornment.Height / 2);

                PutLiveAdornment(adornment);
            });
        }
    }
}
