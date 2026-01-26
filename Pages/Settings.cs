using LibrePad.Classes;
using LibrePad.Utilities;
using TMPro;
using UnityEngine;

namespace LibrePad.Pages
{
    public class Settings : MonoBehaviour
    {
        public void InitializePage()
        {
            Transform pageTransform = transform;
            pageTransform.Find("ChangeTheme").AddComponent<Button>().OnClick += () =>
            {
                Plugin.Configuration.ThemeIndex.Value += 1;
                Plugin.Configuration.ThemeIndex.Value %= 8;
                Plugin.Configuration.Save();

                Tablet.Instance.ApplyTheme();
            };

            pageTransform.Find("ToggleNotifications").AddComponent<Button>().OnClick += () =>
            {
                Plugin.Configuration.Notifications.Value = !Plugin.Configuration.Notifications.Value;
                Plugin.Configuration.Save();

                pageTransform.Find("ToggleNotifications/Text").GetComponent<TextMeshPro>().SafeSetText(Plugin.Configuration.Notifications.Value ? "Disable Notifications" : "Enable Notifications");
            };
        }
    }
}
