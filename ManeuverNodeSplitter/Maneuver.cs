using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ManeuverNodeSplitter
{
    public class Maneuver
    {
        public Vector3d DeltaV;
        public double UT;

        public Maneuver() { }
        public Maneuver(ManeuverNode node)
        {
            this.DeltaV = node.DeltaV;
            this.UT = node.UT;
        }
    }
}
