using System;
using System.Collections.Generic;
using UnityEngine;

namespace ManeuverNodeSplitter
{
    abstract class BaseNodeSplitter : INodeSplitter
    {
        private List<String> values = new List<String>();
        private bool repeat;
        private string repeatLimit;
        private bool supportsRepeat;

        protected abstract string GetRowLabel();

        protected abstract void AdjustNode(
            ManeuverNode originalNode, Maneuver original, double originalPeriod, double originalMagnitude, ManeuverNode node, double value, double splitDv);

        protected abstract bool ValidateInput(ManeuverNode originalNode, List<double> inputValues);

        protected BaseNodeSplitter(INodeSplitterAddon addon, bool supportsRepeat) : base(addon)
        {
            this.supportsRepeat = supportsRepeat;
        }

        internal override void DrawWindowImpl(int wid)
        {
            Color bgColor = GUI.backgroundColor;

            GUILayout.BeginVertical();

            if(values.Count == 0)
                values.Add("");

            int toRemove = -1;
            for(int index = 0; index < values.Count; index += 1)
            {
                if(DrawInputRow(index))
                {
                    toRemove = index;
                }
            }
            if(toRemove >= 0)
            {
                addon.ResetWindow();
                values.RemoveAt(toRemove);
            }

            GUILayout.BeginHorizontal();
            if(supportsRepeat)
            {
                bool oldRepeat = repeat;
                repeat = GUILayout.Toggle(repeat, "Repeat", GUILayout.ExpandWidth(true));
                if(repeat != oldRepeat)
                {
                    addon.ResetWindow();
                }
            }
            else
            {
                GUILayout.Label(" ", GUILayout.ExpandWidth(true));
            }
            GUI.backgroundColor = Color.green;
            if(GUILayout.Button("+", GUILayout.Width(30)))
            {
                values.Add(values[values.Count - 1]);
            }
            GUI.backgroundColor = bgColor;
            GUILayout.EndHorizontal();

            if(supportsRepeat && repeat)
            {
                if(repeatLimit == null)
                    repeatLimit = "";

                GUILayout.BeginHorizontal();
                GUILayout.Label("to", GUILayout.Width(30));
                repeatLimit = GUILayout.TextField(repeatLimit, GUILayout.ExpandWidth(true));
                GUILayout.Label("km", GUILayout.Width(30));
                GUILayout.Label("", GUILayout.Width(30));
                GUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();
        }

        protected virtual bool DrawInputRow(int index)
        {
            Color bgColor = GUI.backgroundColor;

            bool toRemove = false;
            GUILayout.BeginHorizontal();
            values[index] = GUILayout.TextField(values[index], GUILayout.ExpandWidth(true));
            GUILayout.Label(GetRowLabel(), GUILayout.Width(30));
            if(values.Count == 1)
            {
                GUILayout.Label(" ", GUILayout.Width(30));
            }
            else
            {
                GUI.backgroundColor = Color.red;
                if(GUILayout.Button("X", GUILayout.Width(30)))
                {
                    toRemove = true;
                }
                GUI.backgroundColor = bgColor;
            }
            GUILayout.EndHorizontal();

            return toRemove;
        }

        internal override void SplitNode()
        {
            LogNodeInfo();
            if(!IsSolverAvailable() || Solver.maneuverNodes.Count == 0)
            {
                ScreenMessages.PostScreenMessage("Unable to comply. Please create a maneuver node first.", 6f, ScreenMessageStyle.UPPER_CENTER);
                return;
            }
            List<double> list = new List<double>();
            foreach(string value in values)
            {
                double v;
                if(!(double.TryParse(value, out v)))
                {
                    ScreenMessages.PostScreenMessage(string.Format("Unable to parse value '{0}'.", value), 6f, ScreenMessageStyle.UPPER_CENTER);
                    return;
                }
                list.Add(v);
            }

            if(supportsRepeat && repeat)
            {
                double v;
                if(!(double.TryParse(repeatLimit, out v)))
                {
                    ScreenMessages.PostScreenMessage(string.Format("Unable to parse value '{0}'.", repeatLimit), 6f, ScreenMessageStyle.UPPER_CENTER);
                    return;
                }
                if(v * 1000 < Solver.maneuverNodes[0].patch.ApA)
                {
                    ScreenMessages.PostScreenMessage("Apoapsis values must be greater than the current apoapsis", 6f, ScreenMessageStyle.UPPER_CENTER);
                    return;
                }
            }

            if(ValidateInput(Solver.maneuverNodes[0], list))
            {
                addon.SaveManeuvers();

                SplitNode(list, false);
            }
        }

        private void SplitNode(List<double> list, bool fix)
        {
            if(IsSolverAvailable() && Solver.maneuverNodes.Count > 0)
            {
                ManeuverNode originalNode = Solver.maneuverNodes[0];
                Maneuver original = new Maneuver(originalNode);
                double originalPeriod = originalNode.patch.period;
                double originalMagnitude = original.DeltaV.magnitude;

                int nodeCount = list.Count;
                double splitDv = 0;
                ManeuverNode node = originalNode;
                for(int index = 0; index < list.Count; index += 1)
                {
                    LogNode("Before " + index, node);

                    AdjustNode(originalNode, original, originalPeriod, originalMagnitude, node, list[index], splitDv);

                    LogNode("After " + index, node);

                    if(node.nextPatch.patchEndTransition == Orbit.PatchTransitionType.ESCAPE)
                    {
                        ScreenMessages.PostScreenMessage("Input values cause ejection in fewer burns than specified!", 8f, ScreenMessageStyle.UPPER_CENTER);
                        Debug.Log(string.Format("Early ejection at index {0}", index));
                        break;
                    }
                    else if(node.nextPatch.patchEndTransition == Orbit.PatchTransitionType.ENCOUNTER)
                    {
                        ScreenMessages.PostScreenMessage("Encounter detected! Unable to split the maneuver as specified!", 8f, ScreenMessageStyle.UPPER_CENTER);
                        Debug.Log(string.Format("Encounter at index {0}", index));
                        break;
                    }

                    splitDv += node.DeltaV.magnitude;
                    node = Solver.AddManeuverNode(node.UT + node.nextPatch.period);
                }
                if(supportsRepeat && repeat)
                {
                    double limit = double.Parse(repeatLimit) * 1000;
                    double value = list[list.Count - 1];
                    while(node.nextPatch.ApA < limit)
                    {
                        LogNode("Before", node);

                        AdjustNode(originalNode, original, originalPeriod, originalMagnitude, node, value, splitDv);

                        LogNode("After", node);
                        if(node.nextPatch.ApA < limit)
                        {
                            nodeCount += 1;
                            splitDv += node.DeltaV.magnitude;
                            node = Solver.AddManeuverNode(node.UT + node.nextPatch.period);
                        }
                    }
                }
                if(fix)
                {
                    // TBD
                }
                else
                {
                    Vector3d dv = original.DeltaV * ((originalMagnitude - splitDv) / originalMagnitude);
                    RotateDeltaV(originalNode, node, ref dv);
                    node.DeltaV = dv;
                    node.solver.UpdateFlightPlan();
                    LogNode("Final", node);
                    double timeDifference = node.UT - original.UT;
                    double orbitsToReverse = Math.Ceiling(timeDifference / originalPeriod);

                    DebugLog("UT: {0} time diff: {1} orbits to reverse: {2}", Planetarium.GetUniversalTime(), timeDifference, orbitsToReverse);
                    while(orbitsToReverse > 0 && original.UT - orbitsToReverse * originalPeriod < Planetarium.GetUniversalTime())
                    {
                        orbitsToReverse -= 1;
                    }
                    DebugLog("Reversing {0} orbits", orbitsToReverse);
                    if(orbitsToReverse > 0)
                    {
                        double timeToReverse = orbitsToReverse * originalPeriod;
                        for(int index = 0; index <= nodeCount; index += 1)
                        {
                            ManeuverNode mn = Solver.maneuverNodes[index];
                            Debug.Log(string.Format("Moving node {0} ({1}) back {2} from {3}", index, mn.DeltaV.magnitude, timeToReverse, mn.UT));
                            mn.UT -= timeToReverse;
                            mn.solver.UpdateFlightPlan();
                        }
                    }
                    double dt = original.UT - node.UT;
                    if(dt < 0)
                    {
                        dt = dt * -1;
                        ScreenMessages.PostScreenMessage(
                            string.Format("Final ejection will happen {0} later than originally planned", DurationToDisplayString(dt)),
                            8f, ScreenMessageStyle.UPPER_CENTER);
                    }
                    else
                    {
                        ScreenMessages.PostScreenMessage(
                            string.Format("Final ejection will happen {0} earlier than originally planned", DurationToDisplayString(dt)),
                            8f, ScreenMessageStyle.UPPER_CENTER);
                    }
                }
                Debug.Log("Done!");
            }
        }
    }
}
