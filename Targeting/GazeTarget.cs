using System.Collections.Generic;

namespace VSAtAGlance.Targeting
{
    public class GazeTarget
    {
        public GazeTargetLocation DefinitionLocation { get; set; }
        public List<GazeTargetLocation> GazedLocations { get; set; } = new List<GazeTargetLocation>();
        public double Weight { get; set; } = 1.0;
        public object DataModel { get; set; }
    }
    public class GazeTargetLocation
    {
        public int Start { get; set; }
        public int Length { get; set; }

        public GazeTargetLocation(int start, int length)
        {
            Start = start;
            Length = length;
        }
    }
    public class GazeTarget<T> : GazeTarget
    {
        public new T DataModel { get { return (T)base.DataModel; } set { base.DataModel = value; } }
    }
}
