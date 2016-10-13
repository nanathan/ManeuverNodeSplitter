using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ManeuverNodeSplitter
{
    class NodeSplitterByPeriod : BaseNodeSplitter
    {
        internal NodeSplitterByPeriod(INodeSplitterAddon addon) : base(addon, false)
        {
        }

        internal override string GetName()
        {
            return "by Period";
        }

        protected override string GetRowLabel()
        {
            return "min";
        }

        protected override bool ValidateInput(ManeuverNode originalNode, List<double> inputValues)
        {
            double period = originalNode.patch.period;
            double prior = period;

            foreach(double input in inputValues)
            {
                if(input * 60 < period)
                {
                    ScreenMessages.PostScreenMessage("Periods must be greater than the current orbital period", 6f, ScreenMessageStyle.UPPER_CENTER);
                    return false;
                }
                if(!(input * 60 > prior))
                {
                    ScreenMessages.PostScreenMessage("Periods must be listed in increasing order", 6f, ScreenMessageStyle.UPPER_CENTER);
                    return false;
                }
                prior = input * 60;
            }

            return true;
        }

        protected override void AdjustNode(
            ManeuverNode originalNode, Maneuver original, double originalPeriod, double originalMagnitude, ManeuverNode node, double value, double splitDv)
        {
            double fraction;
            double minFraction = 0;
            double maxFraction = 1;

            double targetPeriod = value * 60;

            for(int iteration = 0; iteration < MNSSettings.Instance.splitByPeriodIterations; iteration += 1)
            {
                fraction = (minFraction + maxFraction) / 2;
                Vector3d dv = original.DeltaV * fraction;
                if(node != originalNode)
                {
                    RotateDeltaV(originalNode, node, ref dv);
                }
                node.DeltaV = dv;
                node.solver.UpdateFlightPlan();

                if(node.nextPatch.ApA > 0 && node.nextPatch.period < targetPeriod)
                    minFraction = fraction;
                else
                    maxFraction = fraction;
            }
        }
    }
}
