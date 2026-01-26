
using System.IO;
using System.Reflection;
using UnityEngine;

namespace LibrePad.Utilities
{
    public class Assets
    {
        private static AssetBundle assetBundle;
        private static void LoadAssetBundle()
        {
            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"{PluginInfo.ClientResourcePath}.librepad");
            if (stream != null)
                assetBundle = AssetBundle.LoadFromStream(stream);
            else
                Debug.LogError("Failed to load assetbundle");
        }

        public static T LoadObject<T>(string assetName) where T : Object
        {
            if (assetBundle == null)
                LoadAssetBundle();

            T gameObject = Object.Instantiate(assetBundle.LoadAsset<T>(assetName));
            return gameObject;
        }

        public static T LoadAsset<T>(string assetName) where T : Object
        {
            if (assetBundle == null)
                LoadAssetBundle();

            T gameObject = assetBundle.LoadAsset(assetName) as T;
            return gameObject;
        }
    }
}