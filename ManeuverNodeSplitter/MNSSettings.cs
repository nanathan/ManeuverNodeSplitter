using System;
using UnityEngine;

namespace ManeuverNodeSplitter
{
    class MNSSettings
    {
        private static MNSSettings _instance;
        public static MNSSettings Instance {
            get {
                if(_instance == null)
                {
                    _instance = new MNSSettings();
                    _instance.Initialize();
                }
                return _instance;
            }
        }

        public GUISkin Skin { get; private set; }
        public string defaultMode { get; private set; }
        public int splitByApoIterations { get; private set; }
        public int splitByBurnTimeIterations { get; private set; }
        public int splitByPeriodIterations { get; private set; }

        private void Initialize()
        {
            Debug.Log("Initializing ManeuverNodeSplitter settings");

            // Defaults
            Skin = HighLogic.Skin;
            defaultMode = "by dV";
            splitByApoIterations = 18;
            splitByBurnTimeIterations = 18;
            splitByPeriodIterations = 18;

            // Load from configuration file
            ConfigNode[] nodes = GameDatabase.Instance.GetConfigNodes("MANEUVER_NODE_SPLITTER");
            if(nodes != null)
            {
                foreach(ConfigNode node in nodes)
                {
                    ConfigNode settings = node.GetNode("SETTINGS");
                    if(settings != null)
                    {
                        Debug.Log("[MNS] Settings node is not null");
                        string skin = settings.GetValue("skin");
                        Debug.Log(string.Format("[MNS] Skin configuration: {0}", skin));
                        if(string.Compare(skin, "unity", true) == 0)
                        {
                            Skin = GUI.skin;
                        }
                        else if(string.Compare(skin, "ksp", true) == 0)
                        {
                            Skin = HighLogic.Skin;
                        }
                        else if(skin != null && skin != "")
                        {
                            GUISkin s = AssetBase.GetGUISkin(skin);
                            if(s != null)
                            {
                                Skin = s;
                            }
                            else
                            {
                                Debug.Log("Searching for skin " + skin);
                                GUISkin[] skins = Resources.FindObjectsOfTypeAll<GUISkin>();
                                foreach(GUISkin gs in skins)
                                {
                                    if(string.Compare(skin, gs.name, true) == 0)
                                    {
                                        Skin = gs;
                                    }
                                }
                            }
                        }

                        string mode = settings.GetValue("defaultMode");
                        if(mode != null && mode.Trim().Length > 0)
                        {
                            defaultMode = mode.Trim();
                        }

                        int iterations;
                        if(int.TryParse(settings.GetValue("splitByApoIterations"), out iterations) && iterations > 0)
                        {
                            splitByApoIterations = iterations;
                        }

                        if(int.TryParse(settings.GetValue("splitByBurnTimeIterations"), out iterations) && iterations > 0)
                        {
                            splitByBurnTimeIterations = iterations;
                        }

                        if(int.TryParse(settings.GetValue("splitByPeriodIterations"), out iterations) && iterations > 0)
                        {
                            splitByPeriodIterations = iterations;
                        }
                    }
                }
            }

            Debug.Log(string.Format("[MNS] skin: {0} ({1})", Skin.label, Skin.name));
        }
    }
}
