using System;
using UnityEngine;

namespace ManeuverNodeSplitter
{
    abstract class INodeSplitter
    {
        protected PatchedConicSolver Solver { get { return FlightGlobals.ActiveVessel == null ? null : FlightGlobals.ActiveVessel.patchedConicSolver; } }

        protected INodeSplitterAddon addon;

        internal abstract string GetName();
        internal abstract void DrawWindowImpl(int wid);
        internal abstract void SplitNode();

        protected INodeSplitter(INodeSplitterAddon addon)
        {
            this.addon = addon;
        }

        internal void DrawWindow(int wid)
        {
            addon.DrawHeader();

            DrawWindowImpl(wid);

            addon.DrawFooter();

            GUI.DragWindow();
        }

        // TBD I'm sure there's a cleaner way to do this
        protected static string DurationToDisplayString(double duration)
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
        protected void RotateDeltaV(ManeuverNode original, ManeuverNode node, ref Vector3d dv)
        {
            DebugLog("dV before {0},{1},{2}", dv.x, dv.y, dv.z);
            dv = Quaternion.Inverse(node.nodeRotation) * original.nodeRotation * dv;
            DebugLog("dV after {0},{1},{2}", dv.x, dv.y, dv.z);
        }

        protected bool IsSolverAvailable()
        {
            return HighLogic.LoadedSceneIsFlight && Solver != null;
        }

        protected void DebugLog(string format, params object[] args)
        {
#if DEBUG
            Debug.Log(string.Format(format, args));
#endif
        }

        protected void LogNodeInfo()
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

        protected void LogNode(string message, ManeuverNode node)
        {
#if DEBUG
            //Quaternion rot = node.nodeRotation;
            //Vector3 angles = rot.eulerAngles;
            Orbit orbit = node.nextPatch;
            Debug.Log(string.Format("{0} Node dv:{1} ut:{2} next per:{3} per:{4}", message, node.DeltaV.magnitude, node.UT, node.nextPatch.period, node.patch.period));
            //Debug.Log(string.Format("{0} Quat w:{1} x:{2} y:{3} z:{4}", message, rot.w, rot.x, rot.y, rot.z));
            //Debug.Log(string.Format("{0} Euler x:{1} y:{2} z:{3}", message, angles.x, angles.y, angles.z));
            Debug.Log(string.Format("{0} Orbit inc:{1} ecc:{2} sma:{3} end:{4}", message, orbit.inclination, orbit.eccentricity, orbit.semiMajorAxis, orbit.patchEndTransition));
#endif
        }
    }
}
