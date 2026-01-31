using BepInEx;
using LibrePad.Patches;
using LibrePad.Utilities;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using UnityEngine;

namespace LibrePad
{
    [BepInPlugin(PluginInfo.GUID, PluginInfo.Name, PluginInfo.Version)]
    public class Plugin : BaseUnityPlugin
    {
        public static Configuration Configuration;
        public static bool Outdated;

        void CheckVersion()
        {
            WebRequest request = WebRequest.Create("https://raw.githubusercontent.com/iiDk-the-actual/LibrePad/refs/heads/master/PluginInfo.cs");
            WebResponse response = request.GetResponse();
            Stream data = response.GetResponseStream();

            if (data == null)
                return;
            
            using StreamReader sr = new StreamReader(data);
            string html = sr.ReadToEnd();

            var match = Regex.Match(html, @"public const string Version\s*=\s*""([^""]+)"";");
            if (!match.Success)
                return;

            string version = match.Groups[1].Value;
            if (PluginInfo.Version != version)
                Outdated = true;
        }

        void Awake()
        {
            CheckVersion();
            Binding.GetBindFromFile(Directory.Exists(Binding.CFilePath) ? Binding.CFilePath : Binding.DFilePath);

            GameObject loader = new GameObject("LibrePad");
            DontDestroyOnLoad(loader);

            Configuration = new Configuration(Config);
            PatchHandler.PatchAll();
            loader.AddComponent<Handler>();
        }
    }
}
