using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using System;
using System.Linq;
using System.Windows;

namespace VSAtAGlance.Layers
{
    public abstract class GazeResponseLayer
    {
        public bool IsAdornmentVisible { get; private set; }
        public bool HasExpired => LastTouched + Timeout < DateTime.Now;
        public DateTime LastTouched { get; set; }
        public virtual TimeSpan Timeout => TimeSpan.FromSeconds(0);
        protected string ContainingLayerName { get; private set; }
        protected Guid Tag { get; private set; } = Guid.NewGuid();
        protected IWpfTextView EditorInstance { get; private set; }

        public bool IsAnchoredToText => Span != default(SnapshotSpan);
        public SnapshotSpan Span { get; protected set; }

        protected GazeResponseLayer(IWpfTextView editorInstance, string containingLayerName, SnapshotSpan? span = null)
        {
            EditorInstance = editorInstance;
            ContainingLayerName = containingLayerName;
            if (span.HasValue)
                Span = span.Value;
            LastTouched = DateTime.Now;
        }

        public virtual void Draw(double x, double y) { }
        public virtual void Draw() { }

        public void Cleanup()
        {
            if (IsAdornmentVisible)
                RemoveLiveAdornment();
        }

        protected IAdornmentLayerElement GetLiveAdornment()
        {
            var adornment = EditorInstance.VisualElement.Dispatcher.Invoke(() => EditorInstance.GetAdornmentLayer(ContainingLayerName).Elements.FirstOrDefault(e => e.Tag.Equals(Tag)));
            return adornment;
        }

        protected void PutLiveAdornment(SnapshotSpan span, UIElement adornment)
        {
            EditorInstance.VisualElement.Dispatcher.Invoke(() => EditorInstance.GetAdornmentLayer(ContainingLayerName).AddAdornment(AdornmentPositioningBehavior.TextRelative, span, Tag, adornment, (s, a) => { IsAdornmentVisible = false; }));
            IsAdornmentVisible = true;
        }

        protected void PutLiveAdornment(UIElement adornment)
        {
            EditorInstance.VisualElement.Dispatcher.Invoke(() => EditorInstance.GetAdornmentLayer(ContainingLayerName).AddAdornment(AdornmentPositioningBehavior.ViewportRelative, null, Tag, adornment, (s, a) => { IsAdornmentVisible = false; }));
            IsAdornmentVisible = true;
        }

        protected void RemoveLiveAdornment()
        {
            EditorInstance.VisualElement.Dispatcher.Invoke(() => EditorInstance.GetAdornmentLayer(ContainingLayerName).RemoveAdornmentsByTag(Tag));
            IsAdornmentVisible = false;
        }
    }
}
