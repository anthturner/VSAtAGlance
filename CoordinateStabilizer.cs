using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace VSAtAGlance
{
    public class CoordinateStabilizer
    {
        public event EventHandler<Point> StabilizedPointAvailable;

        public CoordinateStabilizer() { }

        private List<Point> _recentPoints = new List<Point>();

        public void AddPoint(Point p)
        {
            if (_recentPoints.Count > 10)
                _recentPoints.RemoveRange(0, _recentPoints.Count / 4);
            _recentPoints.Add(p);
            Stabilize();
        }

        private void Stabilize(int radiusPx = 25)
        {
            var pts = _recentPoints.ToArray();
            var mostRecentPoint = _recentPoints.Last();

            var center = LocateGazeCenter(pts);
            if (Point.Subtract(center, mostRecentPoint).Length > radiusPx)
            {
                _recentPoints.RemoveRange(0, (_recentPoints.Count / 4) * 3);
                return;
            }

            var gazePointScores = ScoreFromCenterPoint(center, 25, pts).OrderBy(s => s.Value);

            // if there's only one scorable point, return it
            if (gazePointScores.Count() == 0)
                return;
            else if (gazePointScores.Count() == 1)
                RaiseStabilizedPointAvailable(gazePointScores.First().Key);

            // if the top 2 points are very far away from each other, return the average of the two
            else if (Point.Subtract(gazePointScores.First().Key, gazePointScores.Skip(1).First().Key).Length > gazePointScores.First().Value / 2)
                RaiseStabilizedPointAvailable(LocateGazeCenter(gazePointScores.First().Key, gazePointScores.Skip(1).First().Key));
            else
                RaiseStabilizedPointAvailable(gazePointScores.First().Key);
        }

        private void RaiseStabilizedPointAvailable(Point pt)
        {
            Task.Run(() => StabilizedPointAvailable?.Invoke(this, pt));
        }

        private Point LocateGazeCenter(params Point[] points)
        {
            // todo: outlier detection

            double xAgg = 0, yAgg = 0;
            foreach (var pt in points)
            {
                xAgg += pt.X;
                yAgg += pt.Y;
            }

            var xCenter = xAgg / points.Length;
            var yCenter = yAgg / points.Length;
            return new Point(xCenter, yCenter);
        }

        private Dictionary<Point, double> ScoreFromCenterPoint(Point center, int maxDistance, params Point[] points)
        {
            var results = new Dictionary<Point, double>();
            foreach (var pt in points)
            {
                if (results.ContainsKey(pt))
                    continue;

                var length = Point.Subtract(center, pt).Length;
                if (length <= maxDistance)
                    results.Add(pt, length);
            }
            return results;
        }
    }
}
