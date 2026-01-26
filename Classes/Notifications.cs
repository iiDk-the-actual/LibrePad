using GorillaLocomotion;
using LibrePad.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LibrePad.Classes
{
    public class Notifications : MonoBehaviour
    {
        public static Notifications Instance { get; private set; }

        public GameObject canvas;
        private GameObject mainCamera;
        private Material textMaterial;

        public static string PreviousNotifi;
        public static TextMeshProUGUI notificationText;

        private bool hasInitialized;
        public static bool noRichText;
        public static bool soundOnError;
        public static bool noPrefix;
        public static bool narrateNotifications;

        public static int NotifiCounter;
        private static readonly List<Coroutine> clearCoroutines = new List<Coroutine>();

        private void Start() =>
            Instance = this;

        private void Init()
        {
            mainCamera = Camera.main.gameObject;

            GameObject canvasParent = new GameObject("iiMenu_NotificationParent");
            canvasParent.transform.position = mainCamera.transform.position;

            canvas = new GameObject("Canvas");
            canvas.AddComponent<Canvas>();
            canvas.AddComponent<CanvasScaler>();
            canvas.AddComponent<GraphicRaycaster>();

            Canvas canvasComponent = canvas.GetComponent<Canvas>();
            canvasComponent.enabled = true;
            canvasComponent.renderMode = RenderMode.WorldSpace;
            canvasComponent.worldCamera = mainCamera.GetComponent<Camera>();

            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(5f, 5f);
            canvasRect.position = mainCamera.transform.position;

            canvas.transform.parent = canvasParent.transform;
            canvasRect.localPosition = new Vector3(0f, 0f, 1.6f);
            canvasRect.localScale = Vector3.one;

            Vector3 rotation = canvasRect.rotation.eulerAngles;
            rotation.y = -270f;
            canvasRect.rotation = Quaternion.Euler(rotation);

            textMaterial = new Material(Shader.Find("GUI/Text Shader"));

            notificationText = CreateText(canvas.transform, new Vector3(-1f, -1f, -0.5f),
                new Vector2(450f, 210f), 30, TextAlignmentOptions.BottomLeft);

            StartCoroutine(SetShaderAfterInit());
        }

        private TextMeshProUGUI CreateText(Transform parent, Vector3 localPos, Vector2 size, int fontSize, TextAlignmentOptions anchor)
        {
            GameObject textObj = new GameObject { transform = { parent = parent } };
            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();

            text.SafeSetText("");
            text.fontSize = fontSize;
            text.SafeSetFont(Utilities.Assets.LoadAsset<TMP_FontAsset>("Futura"));
            text.rectTransform.sizeDelta = size;
            text.alignment = anchor;
            text.overflowMode = anchor == TextAlignmentOptions.BottomLeft ? TextOverflowModes.Overflow : TextOverflowModes.Truncate;
            text.rectTransform.localScale = new Vector3(0.00333333333f, 0.00333333333f, 0.33333333f);
            text.rectTransform.localPosition = localPos;
            text.material = textMaterial;
            text.characterSpacing = -9f;

            return text;
        }

        private void FixedUpdate()
        {
            try
            {
                if (!hasInitialized && Camera.main != null)
                {
                    Init();
                    hasInitialized = true;
                }

                canvas.GetComponent<CanvasScaler>().dynamicPixelsPerUnit = 2f;

                canvas.transform.position = mainCamera.transform.TransformPoint(0f, 0f, 1.6f);
                canvas.transform.rotation = mainCamera.transform.rotation * Quaternion.Euler(0, 90, 0);
                canvas.transform.localScale = Vector3.one * GTPlayer.Instance.scale;

                notificationText.Chams();
            }
            catch (Exception e) { Debug.Log(e); }
        }

        /// <summary>
        /// Displays a notification message to the user, with optional customization for display duration and
        /// formatting.
        /// </summary>
        /// <remarks>If the notification text matches the previous notification and notification stacking
        /// is enabled, the notification count is incremented instead of displaying a new message. The method applies
        /// various formatting and translation options based on current settings, and may play a notification sound or
        /// narrate the message if those features are enabled. Rich text support and text casing are also configurable.
        /// This method is thread-unsafe and should be called from the main UI thread.</remarks>
        /// <param name="notificationText">The text of the notification to display. May include rich text formatting tags. If translation is enabled,
        /// the text will be translated before display.</param>
        /// <param name="clearTime">The time, in milliseconds, before the notification is cleared. Specify -1 to use the default notification
        /// decay time.</param>
        public static void SendNotification(string notificationText, int clearTime = 5000)
        {
            if (!Plugin.Configuration.Notifications.Value) return;

            try
            {
                notificationText = notificationText.TrimEnd('\n', '\r');

                if (PreviousNotifi == notificationText)
                {
                    NotifiCounter++;

                    string[] lines = Notifications.notificationText.text.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

                    if (lines.Length > 0)
                    {
                        string lastLine = lines[^1];
                        int counterIndex = lastLine.IndexOf(" <color=grey>(x", StringComparison.Ordinal);
                        if (counterIndex > 0)
                            lastLine = lastLine[..counterIndex];

                        lines[^1] = $"{lastLine} <color=grey>(x{NotifiCounter + 1})</color>";
                        Notifications.notificationText.SafeSetText(string.Join(Environment.NewLine, lines));
                    }

                    if (clearCoroutines.Count > 0)
                        CancelClear(clearCoroutines[0]);
                }
                else
                {
                    NotifiCounter = 0;
                    PreviousNotifi = notificationText;

                    if (!string.IsNullOrEmpty(Notifications.notificationText.text))
                    {
                        string currentText = Notifications.notificationText.text.TrimEnd('\n', '\r');
                        Notifications.notificationText.SafeSetText(currentText + Environment.NewLine + notificationText);
                    }
                    else
                        Notifications.notificationText.SafeSetText(notificationText);
                }

                Instance.StartCoroutine(TrackCoroutine(ClearHolder(clearTime / 1000f)));
            }
            catch (Exception e)
            {
                Debug.LogError($"Notification failed, object probably nil due to third person ; {notificationText} {e.Message}");
            }
        }

        private static AudioClip notificationSound;
        public static void PlayNotificationSound()
        {
            notificationSound ??= Utilities.Assets.LoadAsset<AudioClip>("error");
            Audio.Play2DAudio(notificationSound, 0.3f);
        }
            
        /// <summary>
        /// Clears all active notifications and stops any ongoing notification clearing operations.
        /// </summary>
        /// <remarks>Call this method to immediately remove all notification text and halt any scheduled
        /// notification clearing. This method is typically used to reset the notification system or when notifications
        /// are no longer relevant.</remarks>
        public static void ClearAllNotifications()
        {
            notificationText.SafeSetText("");

            foreach (Coroutine coroutine in clearCoroutines)
                Instance.StopCoroutine(coroutine);

            clearCoroutines.Clear();
        }

        /// <summary>
        /// Removes a specified number of past notification entries from the notification text.
        /// </summary>
        /// <param name="amount">The number of past notification lines to remove. Must be zero or greater. If the value is greater than or
        /// equal to the total number of notification lines, all notifications are cleared.</param>
        public static void ClearPastNotifications(int amount)
        {
            if (string.IsNullOrEmpty(notificationText.text))
                return;

            string[] lines = notificationText.text.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

            if (amount >= lines.Length)
            {
                notificationText.SafeSetText("");
                return;
            }

            List<string> remainingLines = new List<string>();
            for (int i = amount; i < lines.Length; i++)
                remainingLines.Add(lines[i]);

            notificationText.SafeSetText(string.Join(Environment.NewLine, remainingLines));
            notificationText.SafeSetText(notificationText.text.TrimEnd('\n', '\r'));
        }

        private static IEnumerator TrackCoroutine(IEnumerator routine)
        {
            IEnumerator Wrapper()
            {
                Coroutine self = Instance.StartCoroutine(routine);
                clearCoroutines.Add(self);
                yield return self;
                clearCoroutines.Remove(self);
            }

            yield return Wrapper();
        }

        private static IEnumerator ClearHolder(float time = 1f)
        {
            yield return new WaitForSeconds(time);
            ClearPastNotifications(1);
        }

        private IEnumerator SetShaderAfterInit()
        {
            yield return null; yield return null; yield return null; yield return null; yield return null;

            notificationText.Chams();
        }

        private static void CancelClear(Coroutine coroutine)
        {
            if (!clearCoroutines.Contains(coroutine)) return;
            clearCoroutines.Remove(coroutine);
            Instance.StopCoroutine(coroutine);
        }

        private static string RemovePrefix(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            string pattern = @"^<color=grey>\[</color><color=[^>]+>.*?</color><color=grey>\]</color> ";
            return System.Text.RegularExpressions.Regex.Replace(text, pattern, "");
        }
    }
}