using System;
using System.Windows;
using System.Windows.Forms;

namespace VSAtAGlance.EyeTracking
{
    public class SimulatedEyeTrackingProvider : IEyeTrackingProvider
    {
        public event EventHandler<Point> PointAvailable;

        private Timer _mouseHookSamplingTimer = new Timer();

        public SimulatedEyeTrackingProvider()
        {
            _mouseHookSamplingTimer = new Timer() { Interval = 50 };

            _mouseHookSamplingTimer.Tick += (s, e) => PointAvailable?.Invoke(this, new System.Windows.Point(Cursor.Position.X, Cursor.Position.Y));

            _mouseHookSamplingTimer.Start();
        }

        ~SimulatedEyeTrackingProvider()
        {
            if (_mouseHookSamplingTimer != null)
                _mouseHookSamplingTimer.Stop();
        }
    }
}
