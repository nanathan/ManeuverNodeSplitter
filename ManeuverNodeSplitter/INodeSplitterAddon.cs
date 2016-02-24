using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ManeuverNodeSplitter
{
    internal interface INodeSplitterAddon
    {
        void ResetWindow();
        void DrawHeader();
        void DrawFooter();
        List<Maneuver> SaveManeuvers();
    }
}
