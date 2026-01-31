using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace LibrePad.Classes
{
    public class Tablet : MonoBehaviour
    {
        public static void InitializeTablet()
        {
            GameObject tablet = Utilities.Assets.LoadObject<GameObject>("CheckUI");
            tablet.transform.SetParent(VRRig.LocalRig.leftHandTransform.parent, false);
            tablet.AddComponent<Tablet>();
        }

        public enum Page
        {
            None,
            Room,
            Player,
            Media,
            Settings
        }

        public GameObject mainObject;
        private Dictionary<Page, GameObject> pageObjects;

        private Page _currentPage = Page.None;
        public Page CurrentPage
        {
            get => _currentPage;
            set
            {
                if (_currentPage == value) return;
                _currentPage = value;

                foreach (var page in pageObjects)
                    page.Value.SetActive(page.Key == _currentPage);
            }
        }

        public static Tablet Instance { get; private set; }

        public GameObject ui;
        public Material backgroundMaterial;
        public Material buttonMaterial;

        public bool muted;

        public void Start()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            gameObject.AddComponent<Rigidbody>().isKinematic = true;

            Transform uiTransform = transform.Find("Main");
            ui = uiTransform.gameObject;

            uiTransform.Find("Background/Title").GetComponent<TextMeshPro>().text = Plugin.Outdated ? $"LibrePad\n<color=red>[OUTDATED]</color> {PluginInfo.Version}" : $"LibrePad\n{PluginInfo.Version}";

            backgroundMaterial = uiTransform.Find("Background").GetComponent<Renderer>().sharedMaterial;
            buttonMaterial = uiTransform.Find("Sidebar/Room").GetComponent<Renderer>().sharedMaterial;

            ApplyTheme();

            pageObjects = new Dictionary<Page, GameObject> 
            {
                { Page.Room, uiTransform.Find("Room").gameObject },
                { Page.Player, uiTransform.Find("Player").gameObject },
                { Page.Media, uiTransform.Find("Media").gameObject },
                { Page.Settings, uiTransform.Find("Settings").gameObject }
            };

            uiTransform.Find("Room").AddComponent<Pages.Room>().InitializePage();
            uiTransform.Find("Player").AddComponent<Pages.Player>().InitializePage();
            uiTransform.Find("Media").AddComponent<Pages.Media>().InitializePage();
            uiTransform.Find("Settings").AddComponent<Pages.Settings>().InitializePage();

            uiTransform.Find("Sidebar/Room").AddComponent<Button>().OnClick += () => CurrentPage = Page.Room;
            uiTransform.Find("Sidebar/Player").AddComponent<Button>().OnClick += () => CurrentPage = Page.Player;
            uiTransform.Find("Sidebar/Media").AddComponent<Button>().OnClick += () => CurrentPage = Page.Media;
            uiTransform.Find("Sidebar/Microphone").AddComponent<Button>().OnClick += () =>
            {
                muted = !muted;

                uiTransform.Find("Sidebar/Microphone/Muted").gameObject.SetActive(muted);
                uiTransform.Find("Sidebar/Microphone/Unmuted").gameObject.SetActive(!muted);
            };
            uiTransform.Find("Sidebar/Settings").AddComponent<Button>().OnClick += () => CurrentPage = Page.Settings;

            CurrentPage = Page.Room;
            ui.SetActive(false);
        }

        private bool previousLeftGrip;
        public void Update()
        {
            bool leftGrip = Binding.Control(Binding.bind);

            if (leftGrip && !previousLeftGrip)
                ui.SetActive(!ui.activeSelf);

            previousLeftGrip = leftGrip;

            if (GorillaTagger.Instance.myRecorder != null)
                GorillaTagger.Instance.myRecorder.TransmitEnabled = !muted;
        }

        public void ApplyTheme()
        {
            switch (Plugin.Configuration.ThemeIndex.Value)
            {
                case 0:
                    backgroundMaterial.color = new Color32(195, 69, 78, 255);
                    buttonMaterial.color = new Color32(99, 31, 34, 255);
                    break;
                case 1:
                    backgroundMaterial.color = new Color32(193, 127, 69, 255);
                    buttonMaterial.color = new Color32(142, 86, 37, 255);
                    break;
                case 2:
                    backgroundMaterial.color = new Color32(193, 183, 69, 255);
                    buttonMaterial.color = new Color32(142, 133, 37, 255);
                    break;
                case 3:
                    backgroundMaterial.color = new Color32(90, 193, 69, 255);
                    buttonMaterial.color = new Color32(56, 140, 37, 255);
                    break;
                case 4:
                    backgroundMaterial.color = new Color32(68, 141, 191, 255);
                    buttonMaterial.color = new Color32(37, 104, 140, 255);
                    break;
                case 5:
                    backgroundMaterial.color = new Color32(68, 68, 191, 255);
                    buttonMaterial.color = new Color32(37, 37, 137, 255);
                    break;
                case 6:
                    backgroundMaterial.color = new Color32(113, 68, 191, 255);
                    buttonMaterial.color = new Color32(74, 37, 137, 255);
                    break;
                case 7:
                    backgroundMaterial.color = new Color32(191, 68, 158, 255);
                    buttonMaterial.color = new Color32(137, 37, 109, 255);
                    break;
            }
        }
    }
}
