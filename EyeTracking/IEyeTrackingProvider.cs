using System;
using System.Windows;

namespace VSAtAGlance.EyeTracking
{
    public interface IEyeTrackingProvider
    {
        event EventHandler<Point> PointAvailable;
    }
}
