using KSP.UI.Screens;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ManeuverNodeSplitter
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class NodeSplitterAddon : MonoBehaviour, INodeSplitterAddon
    {
        protected PatchedConicSolver Solver { get { return FlightGlobals.ActiveVessel == null ? null : FlightGlobals.ActiveVessel.patchedConicSolver; } }

        private ApplicationLauncherButton launcherButton;
        private IButton button;
        private bool visible;
        private int windowId = 9651;
        private Rect position = new Rect(100, 300, 200, 100);

        private List<INodeSplitter> splitters = new List<INodeSplitter>();
        private INodeSplitter currentSplitter;

        protected List<Maneuver> oldManeuvers = new List<Maneuver>();

        public void Awake()
        {
            splitters.Add(new NodeSplitterByApoapsis(this));
            if(NodeSplitterByBurnTime.Available)
            {
                splitters.Add(new NodeSplitterByBurnTime(this));
            }
            splitters.Add(new NodeSplitterByDeltaV(this));
            splitters.Add(new NodeSplitterByPeriod(this));
            splitters.Sort((l, r) => { return string.Compare(l.GetName(), r.GetName(), true); });
            currentSplitter = splitters[0];
        }

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

        public void ResetWindow()
        {
            position.height = 100;
        }

        private void ToggleVisibility()
        {
            visible = !visible;
        }

        private bool initialized = false;
        private void OnFirstGUI()
        {
            int index = splitters.FindIndex((s) => { return string.Compare(s.GetName(), MNSSettings.Instance.defaultMode, true) == 0; });
            if(index >= 0)
            {
                currentSplitter = splitters[index];
            }
            initialized = true;
        }

        internal void OnGUI()
        {
            if(!initialized)
            {
                OnFirstGUI();
            }
            if(visible && currentSplitter != null)
            {
                GUI.skin = MNSSettings.Instance.Skin;

                position = GUILayout.Window(windowId, position, currentSplitter.DrawWindow, "Node Splitter", GUILayout.ExpandHeight(true));
            }
        }

        public void DrawHeader()
        {
            GUILayout.BeginHorizontal();

            GUIStyle alignCenter = new GUIStyle(MNSSettings.Instance.Skin.label);
            alignCenter.alignment = TextAnchor.MiddleCenter;

            if(GUILayout.Button("<", GUILayout.Width(30)))
                PreviousSplitter();
            GUILayout.Label(currentSplitter.GetName(), alignCenter, GUILayout.ExpandWidth(true));
            if(GUILayout.Button(">", GUILayout.Width(30)))
                NextSplitter();

            GUILayout.EndHorizontal();
        }

        public void DrawFooter()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("", GUILayout.ExpandWidth(true));
            if(oldManeuvers.Count > 0 && oldManeuvers[0].UT > Planetarium.GetUniversalTime())
            {
                if(GUILayout.Button("Undo", GUILayout.Width(70)))
                {
                    UndoSplit();
                    ResetWindow();
                }
            }
            else
            {
                GUILayout.Label(" ", GUILayout.Width(70));
            }
            if(GUILayout.Button("Apply", GUILayout.Width(70)))
            {
                currentSplitter.SplitNode();
            }
            GUILayout.EndHorizontal();
        }

        internal void PreviousSplitter()
        {
            int index = splitters.IndexOf(currentSplitter) - 1;
            if(index < 0)
            {
                index = splitters.Count - 1;
            }
            currentSplitter = splitters[index];
            ResetWindow();
        }

        internal void NextSplitter()
        {
            int index = splitters.IndexOf(currentSplitter) + 1;
            if(index >= splitters.Count)
            {
                index = 0;
            }
            currentSplitter = splitters[index];
            ResetWindow();
        }

        protected bool IsSolverAvailable()
        {
            return HighLogic.LoadedSceneIsFlight && Solver != null;
        }

        public List<Maneuver> SaveManeuvers()
        {
            List<Maneuver> oldSaves = new List<Maneuver>(oldManeuvers);
            oldManeuvers.Clear();
            foreach(ManeuverNode node in Solver.maneuverNodes)
            {
                oldManeuvers.Add(new Maneuver(node));
            }
            return oldSaves;
        }

        private void UndoSplit()
        {
            if(IsSolverAvailable() && oldManeuvers.Count > 0 && oldManeuvers[0].UT > Planetarium.GetUniversalTime())
            {
                List<Maneuver> toRestore = SaveManeuvers();

                while(Solver.maneuverNodes.Count > 0)
                {
                    Solver.maneuverNodes[0].RemoveSelf();
                }
                foreach(Maneuver m in toRestore)
                {
                    ManeuverNode node = Solver.AddManeuverNode(m.UT);
                    node.DeltaV = m.DeltaV;
                    node.solver.UpdateFlightPlan();
                }

                ScreenMessages.PostScreenMessage(string.Format("Replaced flight plan with {0} saved maneuvers.", toRestore.Count), 8f, ScreenMessageStyle.UPPER_CENTER);
            }
        }
    }
}
