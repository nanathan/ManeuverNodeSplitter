using System;
using System.Collections.Generic;
using UnityEngine;

namespace ManeuverNodeSplitter
{
    interface IBurnCalculator
    {
        /* Calculates the burn time for a future maneuver node after the vessel has already
         * used some amount of dv. 
         *
         * priorDv: the amount of dv that will already have been burned before this node
         * additionalDv: the amount of dv for this burn
         */
        double CalculateBurnTime(double additionalDv, double priorDv);
    }

    class NodeSplitterByBurnTime : BaseNodeSplitter
    {
        internal NodeSplitterByBurnTime(INodeSplitterAddon addon) : base(addon, true)
        {
        }

        internal override string GetName()
        {
            return "by Burn Time";
        }

        protected override string GetRowLabel()
        {
            return "sec";
        }

        protected override bool ValidateInput(ManeuverNode originalNode, List<double> inputValues)
        {
            foreach(double input in inputValues)
            {
                if(input <= 0)
                {
                    ScreenMessages.PostScreenMessage("Burn times must be positive", 6f, ScreenMessageStyle.UPPER_CENTER);
                    return false;
                }
            }

            return true;
        }

        protected override void AdjustNode(
            ManeuverNode originalNode, Maneuver original, double originalPeriod, double originalMagnitude, ManeuverNode node, double value, double splitDv)
        {
            double fraction;
            double minFraction = 0;
            double maxFraction = 1;

            IBurnCalculator calculator = GetAvailableCalculator();

            for(int iteration = 0; iteration < MNSSettings.Instance.splitByBurnTimeIterations; iteration += 1)
            {
                fraction = (minFraction + maxFraction) / 2;
                Vector3d dv = original.DeltaV * fraction;
                if(node != originalNode)
                {
                    RotateDeltaV(originalNode, node, ref dv);
                }
                node.DeltaV = dv;
                node.solver.UpdateFlightPlan();

                double burnTime = calculator.CalculateBurnTime(dv.magnitude, splitDv);
                
                if(node.nextPatch.ApA > 0 && burnTime < value)
                    minFraction = fraction;
                else
                    maxFraction = fraction;
            }
        }

        public static bool Available {
            get {
                return KER.KERWrapper.Available;
            }
        }

        private IBurnCalculator GetAvailableCalculator()
        {
            return new KERBurnCalculator();
        }
    }

    class KERBurnCalculator : IBurnCalculator
    {
        private KER.Stage[] stages;

        public KERBurnCalculator()
        {
            stages = KER.KERWrapper.Instance.GetStages();
        }

        public double CalculateBurnTime(double additionalDv, double priorDv)
        {
            if(priorDv > double.Epsilon)
                return TotalBurnTime(additionalDv + priorDv) - TotalBurnTime(priorDv);
            else
                return TotalBurnTime(additionalDv);
        }

        private double TotalBurnTime(double dv)
        {
            double time = 0;
            double dvRemaining = dv;

            for(int index = stages.Length - 1; index >= 0; index -= 1)
            {
                KER.Stage stage = stages[index];

                if(dvRemaining <= double.Epsilon)
                {
                    break;
                }

                if(stage.DeltaV > 0)
                {
                    double dvToBurn = stage.DeltaV > dvRemaining ? dvRemaining : stage.DeltaV;
                    double massFlowRate = stage.Thrust / (stage.Isp * 9.80665);

                    time += stage.TotalMass / massFlowRate * (1.0 - Math.Exp(-dvToBurn * massFlowRate / stage.Thrust));
                    dvRemaining -= dvToBurn;
                }
            }

            if(dvRemaining > double.Epsilon)
            {
                return double.PositiveInfinity;
            }

            return time;
        }
    }
}
