using System;
using System.Collections.Generic;
using UnityEngine;

namespace ManeuverNodeSplitter
{
    using ManeuverNodeSplitter.KER;

    class NodeSplitterByApoapsis : BaseNodeSplitter
    {
        internal NodeSplitterByApoapsis(INodeSplitterAddon addon) : base(addon, false)
        {
        }

        internal override string GetName()
        {
            return "by Apoapsis";
        }

        protected override string GetRowLabel()
        {
            return "km";
        }

        protected override bool ValidateInput(ManeuverNode originalNode, List<double> inputValues)
        {
            double apoapsis = originalNode.patch.ApA;
            double prior = apoapsis;

            foreach(double input in inputValues)
            {
                if(input * 1000 < apoapsis)
                {
                    ScreenMessages.PostScreenMessage("Apoapsis values must be greater than the current apoapsis", 6f, ScreenMessageStyle.UPPER_CENTER);
                    return false;
                }
                if(!(input * 1000 > prior))
                {
                    ScreenMessages.PostScreenMessage("Apoapsis values must be listed in increasing order", 6f, ScreenMessageStyle.UPPER_CENTER);
                    return false;
                }
                prior = input * 1000;
            }

            return true;
        }

        protected override void AdjustNode(
            ManeuverNode originalNode, Maneuver original, double originalPeriod, double originalMagnitude, ManeuverNode node, double value, double splitDv)
        {
            double fraction;
            double minFraction = 0;
            double maxFraction = 1;

            double targetApo = value * 1000;

            for(int iteration = 0; iteration < MNSSettings.Instance.splitByApoIterations; iteration += 1)
            {
                fraction = (minFraction + maxFraction) / 2;
                Vector3d dv = original.DeltaV * fraction;
                if(node != originalNode)
                {
                    RotateDeltaV(originalNode, node, ref dv);
                }
                node.DeltaV = dv;
                node.solver.UpdateFlightPlan();

                if(node.nextPatch.ApA > 0 && node.nextPatch.ApA < targetApo)
                    minFraction = fraction;
                else
                    maxFraction = fraction;
            }
        }
    }
}
