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

        private void Initialize()
        {
            Debug.Log("Initializing ManeuverNodeSplitter settings");
            Skin = HighLogic.Skin;

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
                    }
                }
            }

            Debug.Log(string.Format("[MNS] skin: {0} ({1})", Skin.label, Skin.name));
        }
    }
}
