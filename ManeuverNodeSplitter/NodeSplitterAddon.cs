using System;
using System.Collections.Generic;
using UnityEngine;

namespace ManeuverNodeSplitter
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class NodeSplitterAddon : MonoBehaviour
    {
        private ApplicationLauncherButton launcherButton;
        private IButton button;
        private bool visible;
        private int windowId = 9651;
        private Rect position = new Rect(100, 300, 150, 100);
        private List<String> values = new List<String>();

        private List<Maneuver> oldManeuvers = new List<Maneuver>();

        private PatchedConicSolver Solver { get { return FlightGlobals.ActiveVessel == null ? null : FlightGlobals.ActiveVessel.patchedConicSolver; } }

        public void Start()
        {
            if(ToolbarManager.ToolbarAvailable)
            {
                button = ToolbarManager.Instance.add("ManeuverNodeSplitter", "GUI");
                button.TexturePath = "NodeSplitter/ejection24";
                button.ToolTip = "Maneuver Node Splitter";
                button.Enabled = true;
                button.OnClick += (e) => { ToggleVisibility(); };
            }
            else
            {
                Texture2D texture = GameDatabase.Instance.GetTexture("NodeSplitter/ejection38", false);
                if(texture != null)
                {
                    launcherButton = ApplicationLauncher.Instance.AddModApplication(
                        ToggleVisibility, ToggleVisibility, null, null, null, null, ApplicationLauncher.AppScenes.MAPVIEW, texture);
                }
            }
        }

        public void OnDestroy()
        {
            if(button != null)
            {
                button.Destroy();
                button = null;
            }
            if(launcherButton != null)
            {
                ApplicationLauncher.Instance.RemoveModApplication(launcherButton);
                launcherButton = null;
            }
        }

        private void ToggleVisibility()
        {
            visible = !visible;
            if(visible)
                RenderingManager.AddToPostDrawQueue(349, Draw);
            else
                RenderingManager.RemoveFromPostDrawQueue(349, Draw);
        }

        internal void Draw()
        {
            position = GUILayout.Window(windowId, position, DrawWindow, "Node Splitter", GUILayout.ExpandHeight(true));
        }

        internal void DrawWindow(int wid)
        {
            GUILayout.BeginVertical();

            if(values.Count == 0)
                values.Add("");

            int toRemove = -1;
            for(int index = 0; index < values.Count; index += 1)
            {
                GUILayout.BeginHorizontal();
                values[index] = GUILayout.TextField(values[index], GUILayout.Width(90));
                if(values.Count == 1)
                {
                    GUILayout.Label(" ", GUILayout.Width(40));
                }
                else
                {
                    if(GUILayout.Button("x", GUILayout.Width(40)))
                    {
                        toRemove = index;
                    }
                }
                GUILayout.EndHorizontal();
            }
            if(toRemove >= 0)
            {
                position.height = 100;
                values.RemoveAt(toRemove);
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label(" ", GUILayout.Width(90));
            if(GUILayout.Button("+", GUILayout.Width(40)))
            {
                values.Add("");
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if(oldManeuvers.Count > 0 && oldManeuvers[0].UT > Planetarium.GetUniversalTime())
            {
                if(GUILayout.Button("Undo", GUILayout.Width(65)))
                {
                    UndoSplit();
                    position.height = 100;
                }
            }
            else
            {
                GUILayout.Label(" ", GUILayout.Width(65));
            }
            if(GUILayout.Button("Apply", GUILayout.Width(65)))
            {
                SplitNode();
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        private void UndoSplit()
        {
            if(IsSolverAvailable() && oldManeuvers.Count > 0 && oldManeuvers[0].UT > Planetarium.GetUniversalTime())
            {
                while(Solver.maneuverNodes.Count > 0)
                {
                    Solver.RemoveManeuverNode(Solver.maneuverNodes[0]);
                }
                foreach(Maneuver m in oldManeuvers)
                {
                    ManeuverNode node = Solver.AddManeuverNode(m.UT);
                    node.OnGizmoUpdated(m.DeltaV, m.UT);
                }
                oldManeuvers.Clear();
            }
        }

        private void SplitNode()
        {
            LogNodeInfo();
            if(!IsSolverAvailable() || Solver.maneuverNodes.Count == 0)
            {
                ScreenMessages.PostScreenMessage("Unable to comply. Please create a maneuver node first.", 6f, ScreenMessageStyle.UPPER_CENTER);
                return;
            }
            List<double> dvs = new List<double>();
            foreach(string value in values)
            {
                double dv;
                if(!(double.TryParse(value, out dv)))
                {
                    ScreenMessages.PostScreenMessage(string.Format("Unable to parse value '{0}'.", value), 6f, ScreenMessageStyle.UPPER_CENTER);
                    return;
                }
                dvs.Add(dv);
            }

            foreach(ManeuverNode node in Solver.maneuverNodes)
            {
                oldManeuvers.Add(new Maneuver(node));
            }
            SplitNode(dvs, false);
        }

        private void SplitNode(List<double> dvs, bool fix)
        {
            if(IsSolverAvailable() && Solver.maneuverNodes.Count > 0)
            {
                ManeuverNode originalNode = Solver.maneuverNodes[0];
                Maneuver original = new Maneuver(originalNode);
                double originalPeriod = originalNode.patch.period;
                double originalMagnitude = original.DeltaV.magnitude;

                double splitDv = 0;
                ManeuverNode node = originalNode;
                for(int index = 0; index < dvs.Count; index += 1)
                {
                    LogNode("Before " + index, node);
                    Vector3d dv = original.DeltaV * (dvs[index] / originalMagnitude);
                    if(node != originalNode)
                    {
                        RotateDeltaV(originalNode, node, ref dv);
                    }
                    node.OnGizmoUpdated(dv, node.UT);
                    LogNode("After " + index, node);
                    node = Solver.AddManeuverNode(node.UT + node.nextPatch.period);
                    splitDv += dvs[index];
                }
                if(fix)
                {
                    // TBD
                }
                else
                {
                    Vector3d dv = original.DeltaV * ((originalMagnitude - splitDv) / originalMagnitude);
                    RotateDeltaV(originalNode, node, ref dv);
                    node.OnGizmoUpdated(dv, node.UT);
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
                        for(int index = 0; index <= dvs.Count; index += 1)
                        {
                            ManeuverNode mn = Solver.maneuverNodes[index];
                            Debug.Log(string.Format("Moving node {0} ({1}) back {2} from {3}", index, mn.DeltaV.magnitude, timeToReverse, mn.UT));
                            mn.OnGizmoUpdated(mn.DeltaV, mn.UT - timeToReverse);
                        }
                    }
                    ManeuverNode finalNode = Solver.maneuverNodes[dvs.Count];
                    double dt = original.UT - finalNode.UT;
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

        // TBD I'm sure there's a cleaner way to do this
        private static string DurationToDisplayString(double duration)
        {
            if(duration < 60)
                return string.Format("{0:F1}s", duration);
            else if(duration < 3600)
            {
                int minutes = (int) Math.Floor(duration / 60);
                double remainder = duration - minutes * 60;
                if(remainder > 1)
                    return string.Format("{0}m {1}", minutes, DurationToDisplayString(remainder));
                else
                    return string.Format("{0}m", minutes);
            }
            else if(duration < 21600)
            {
                int hours = (int) Math.Floor(duration / 3600);
                double remainder = duration - hours * 3600;
                if(remainder > 1)
                    return string.Format("{0}h {1}", hours, DurationToDisplayString(remainder));
                else
                    return string.Format("{0}h", hours);
            }
            else
            {
                int days = (int) Math.Floor(duration / 21600);
                double remainder = duration - days * 21600;
                if(remainder > 1)
                    return string.Format("{0}d {1}", days, DurationToDisplayString(remainder));
                else
                    return string.Format("{0}d", days);
            }
        }

        /* Nodes after the first will be in a different orbital plane if the maneuver is
         * not purely a prograde burn, so they must be rotated since the dV is specified
         * relative to the orbit at the point of the maneuver node
         */
        private void RotateDeltaV(ManeuverNode original, ManeuverNode node, ref Vector3d dv)
        {
            DebugLog("dV before {0},{1},{2}", dv.x, dv.y, dv.z);
            dv = node.nodeRotation.Inverse() * original.nodeRotation * dv;
            DebugLog("dV after {0},{1},{2}", dv.x, dv.y, dv.z);
        }

        private bool IsSolverAvailable()
        {
            return HighLogic.LoadedSceneIsFlight && Solver != null;
        }

        private void DebugLog(string format, params object[] args)
        {
#if DEBUG
            Debug.Log(string.Format(format, args));
#endif
        }

        private void LogNodeInfo()
        {
#if DEBUG
            if(HighLogic.LoadedSceneIsFlight)
            {
                if(FlightGlobals.ActiveVessel == null)
                {
                    Debug.Log("[ManeuverNodeSplitter] active vessel is null");
                }
                else if(Solver == null)
                {
                    Debug.Log("[ManeuverNodeSplitter] solver is null");
                }
                else
                {
                    Debug.Log("[ManeuverNodeSplitter] nodes: " + Solver.maneuverNodes.Count);
                }
            }
            if(IsSolverAvailable())
            {
                foreach(ManeuverNode node in Solver.maneuverNodes)
                {
                    LogNode("", node);
                }
            }
#endif
        }

        private void LogNode(string message, ManeuverNode node)
        {
#if DEBUG
            Quaternion rot = node.nodeRotation;
            Vector3 angles = rot.eulerAngles;
            Debug.Log(string.Format("{0} Node dv:{1} ut:{2} next per:{3} per:{4}", message, node.DeltaV.magnitude, node.UT, node.nextPatch.period, node.patch.period));
            Debug.Log(string.Format("{0} Quat w:{1} x:{2} y:{3} z:{4}", message, rot.w, rot.x, rot.y, rot.z));
            Debug.Log(string.Format("{0} Euler x:{1} y:{2} z:{3}", message, angles.x, angles.y, angles.z));
#endif
        }
    }
}
