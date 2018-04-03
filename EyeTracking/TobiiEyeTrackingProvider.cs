using System;
using System.Threading.Tasks;
using System.Windows;
using Tobii.Interaction;

namespace VSAtAGlance.EyeTracking
{
    public class TobiiEyeTrackingProvider : IEyeTrackingProvider
    {
        public event EventHandler<Point> PointAvailable;

        private Host gazeHost;
        private GazePointDataStream gazePointDataStream;

        public TobiiEyeTrackingProvider()
        {
            gazeHost = new Host();
            gazePointDataStream = gazeHost.Streams.CreateGazePointDataStream(Tobii.Interaction.Framework.GazePointDataMode.LightlyFiltered);
            gazePointDataStream.Next += (s, e) =>
            {
                Task.Run(() =>
                {
                    PointAvailable?.Invoke(this, new Point(e.Data.X, e.Data.Y));
                });
            };
        }

        ~TobiiEyeTrackingProvider()
        {
            gazeHost.DisableConnection();
        }
    }
}
