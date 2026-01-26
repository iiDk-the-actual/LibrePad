using BepInEx;
using LibrePad.Patches;
using LibrePad.Utilities;
using UnityEngine;

namespace LibrePad
{
    [BepInPlugin(PluginInfo.GUID, PluginInfo.Name, PluginInfo.Version)]
    public class Plugin : BaseUnityPlugin
    {
        public static Configuration Configuration;

        void Awake()
        {
            GameObject loader = new GameObject("LibrePad");
            DontDestroyOnLoad(loader);

            Configuration = new Configuration(Config);
            PatchHandler.PatchAll();
            loader.AddComponent<Handler>();
        }
    }
}
