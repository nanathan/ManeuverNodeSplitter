using System;
using System.Collections.Generic;
using UnityEngine;

namespace ManeuverNodeSplitter
{
    class NodeSplitterByDeltaV : BaseNodeSplitter
    {
        internal NodeSplitterByDeltaV(INodeSplitterAddon addon) : base(addon, true)
        {
        }

        internal override string GetName()
        {
            return "by dV";
        }

        protected override string GetRowLabel()
        {
            return "m/s";
        }

        protected override bool ValidateInput(ManeuverNode originalNode, List<double> inputValues)
        {
            foreach(double input in inputValues)
            {
                if(input <= 0)
                {
                    ScreenMessages.PostScreenMessage("Delta-V values must be greater than zero", 6f, ScreenMessageStyle.UPPER_CENTER);
                    return false;
                }
            }
            return true;
        }

        protected override void AdjustNode(
            ManeuverNode originalNode, Maneuver original, double originalPeriod, double originalMagnitude, ManeuverNode node, double value, double splitDv)
        {
            Vector3d dv = original.DeltaV * (value / originalMagnitude);
            if(node != originalNode)
            {
                RotateDeltaV(originalNode, node, ref dv);
            }
            node.DeltaV = dv;
            node.solver.UpdateFlightPlan();
        }
    }
}
