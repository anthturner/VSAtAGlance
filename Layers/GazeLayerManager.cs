using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using System;
using System.Collections.Generic;
using System.Linq;

namespace VSAtAGlance.Layers
{
    public class GazeLayerManager
    {
        private static Dictionary<IWpfTextView, GazeLayerManager> _layerManagers = new Dictionary<IWpfTextView, GazeLayerManager>();

        public static GazeLayerManager Get(IWpfTextView editorInstance)
        {
            if (!_layerManagers.ContainsKey(editorInstance))
                _layerManagers[editorInstance] = new GazeLayerManager(editorInstance);
            return _layerManagers[editorInstance];
        }

        // ----------

        private IWpfTextView EditorInstance { get; set; }

        /// <summary>
        /// Layers drawn exactly once per incoming tracking point, cleaned up automatically each cycles
        /// </summary>
        public List<GazeResponseLayer> SingletonLayers { get; set; }

        /// <summary>
        /// Layers that are responsible for their own lifetimes
        /// </summary>
        public List<GazeResponseLayer> Layers { get; set; }

        private GazeLayerManager(IWpfTextView editorInstance)
        {
            EditorInstance = editorInstance;
            SingletonLayers = new List<GazeResponseLayer>();
            Layers = new List<GazeResponseLayer>();
        }

        public void ScrubLayers()
        {
            var newLayers = new List<GazeResponseLayer>();
            foreach (var layer in Layers)
                if (layer.HasExpired)
                    layer.Cleanup();
                else newLayers.Add(layer);
            Layers = newLayers;
        }

        public bool IsTracking(SnapshotSpan span)
        {
            var layer = Layers.FirstOrDefault(l => l.IsAnchoredToText && l.Span.OverlapsWith(span));
            if (layer == null) return false;
            layer.LastTouched = DateTime.Now;
            return true;
        }

        public void DrawSingletonLayers(double x, double y)
        {
            foreach (var layer in SingletonLayers)
            {
                layer.Cleanup();
                layer.Draw(x, y);
            }
        }

        public void Draw()
        {
            foreach (var layer in Layers.Where(l => !l.IsAdornmentVisible))
                layer.Draw();
        }
    }
}
