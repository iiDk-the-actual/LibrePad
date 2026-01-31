using GorillaNetworking;
using HarmonyLib;
using LibrePad.Classes;
using LibrePad.Pages;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace LibrePad.Patches
{
    [HarmonyPatch(typeof(VRRig), "IUserCosmeticsCallback.OnGetUserCosmetics")]
    public class CosmeticsLoadedPatch
    {
        private static void Postfix(VRRig __instance, string cosmetics)
        {
            List<string> legalMods = new List<string>();
            List<string> illegalMods = new List<string>();

            Dictionary<string, object> customProps = new Dictionary<string, object>();
            foreach (DictionaryEntry dictionaryEntry in __instance.OwningNetPlayer.GetPlayerRef().CustomProperties)
                customProps[dictionaryEntry.Key.ToString().ToLower()] = dictionaryEntry.Value;

            foreach (var mod in Player.modDictionary.Where(mod => customProps.ContainsKey(mod.Key.ToLower())))
            {
                if (mod.Value.legal)
                    legalMods.Add(mod.Value.name);
                else
                    illegalMods.Add(mod.Value.name);
            }

            CosmeticsController.CosmeticSet cosmeticSet = __instance.cosmeticSet;
            if (cosmeticSet.items.Any(cosmetic => !cosmetic.isNullItem && !__instance.rawCosmeticString.Contains(cosmetic.itemName)))
                illegalMods.Add("Cosmetx");

            if (legalMods.Count > 0 || illegalMods.Count > 0)
                Notifications.SendNotification($"<color={(illegalMods.Count > 0 ? "red" : "green")}>{(illegalMods.Count > 0 ? "Cheater" : "Modder")}</color> {__instance.playerNameVisible} has <color=green>{(legalMods.Count > 0 ? $"{legalMods.Count} mod{(legalMods.Count > 1 ? "s" : "")}" : "")}</color>{(legalMods.Count > 0 && illegalMods.Count > 0 ? " and " : "")}<color=red>{(illegalMods.Count > 0 ? $"{illegalMods.Count} cheat{(legalMods.Count > 1 ? "s" : "")}" : "")}</color>");
        }
    }
}