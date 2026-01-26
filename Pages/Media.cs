using GorillaNetworking;
using LibrePad.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using Valve.Newtonsoft.Json;

namespace LibrePad.Pages
{
    public class Media : MonoBehaviour
    {
        private GameObject photo;

        private TextMeshPro title;
        private TextMeshPro artist;

        private TextMeshPro elapsedTime;
        private TextMeshPro endTime;

        private GameObject sliderAmount;
        private GameObject sliderBackground;
        private Material mediaMaterial;

        private GameObject pauseIcon;
        private GameObject playIcon;

        public string quickSongPath;
        public void InitializePage()
        {
            Transform pageTransform = transform;

            photo = pageTransform.Find("Photo").gameObject;
            mediaMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            photo.GetComponent<Renderer>().material = mediaMaterial;

            title = pageTransform.Find("SongTitle").GetComponent<TextMeshPro>();
            artist = pageTransform.Find("SongArtist").GetComponent<TextMeshPro>();

            pauseIcon = pageTransform.Find("PauseButton/Pause").gameObject;
            playIcon = pageTransform.Find("PauseButton/Play").gameObject;

            elapsedTime = pageTransform.Find("Duration/Elapsed").GetComponent<TextMeshPro>();
            endTime = pageTransform.Find("Duration/Length").GetComponent<TextMeshPro>();

            sliderBackground = pageTransform.Find("Duration/Slider").gameObject;
            sliderAmount = pageTransform.Find("Duration/DragAmount").gameObject;

            pageTransform.Find("PauseButton").AddComponent<Classes.Button>().OnClick += () => PauseTrack();

            pageTransform.Find("Previous").AddComponent<Classes.Button>().OnClick += () => PreviousTrack();
            pageTransform.Find("Next").AddComponent<Classes.Button>().OnClick += () => SkipTrack();

            string resourcePath = "LibrePad.Resources.QuickSong.exe";
            quickSongPath = Path.Combine(Path.GetTempPath(), "QuickSong.exe");

            if (File.Exists(quickSongPath))
                File.Delete(quickSongPath);

            using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourcePath);
            using FileStream fs = new FileStream(quickSongPath, FileMode.Create, FileAccess.Write);
            stream.CopyTo(fs);
        }

        private static float updateDataLatency;
        public void Update()
        {
            if (Time.time > updateDataLatency)
            {
                updateDataLatency = Time.time + 5f;
                StartCoroutine(UpdateDataCoroutine());
            }
            
            if (ValidData)
            {
                if (!Paused)
                    ElapsedTime += Time.deltaTime;

                float clampedElapsed = Mathf.Clamp(ElapsedTime, StartTime, EndTime);
                elapsedTime.SafeSetText($"{Mathf.Floor(clampedElapsed / 60)}:{Mathf.Floor(clampedElapsed % 60):00}");
                endTime.SafeSetText($"{Mathf.Floor(EndTime / 60)}:{Mathf.Floor(EndTime % 60):00}");

                pauseIcon.SetActive(!Paused);
                playIcon.SetActive(Paused);

                sliderAmount.transform.localPosition = new Vector3(Mathf.Lerp(-sliderBackground.transform.localScale.x * 5f, 0f, clampedElapsed / EndTime), sliderBackground.transform.localPosition.y, sliderAmount.transform.localPosition.z);
                sliderAmount.transform.localScale = new Vector3(Mathf.Lerp(0f, sliderBackground.transform.localScale.x, clampedElapsed / EndTime), sliderBackground.transform.localScale.y, sliderBackground.transform.localScale.z);

                title.SafeSetText(Title);
                artist.SafeSetText(Artist);

                if (mediaMaterial.GetTexture("_BaseMap") != Icon)
                    mediaMaterial.SetTexture("_BaseMap", Icon);
            }
        }

        public void PublicHop()
        {
            GorillaNetworkJoinTrigger trigger = PhotonNetworkController.Instance.currentJoinTrigger ?? GorillaComputer.instance.GetJoinTriggerForZone("forest");
            PhotonNetworkController.Instance.AttemptToJoinPublicRoom(trigger);
        }

        public void PrivateHop()
        {
            string roomName = NetworkSystem.Instance.GetMyNickName();

            if (roomName.Length > 6)
                roomName = roomName[..6];

            PhotonNetworkController.Instance.AttemptToJoinSpecificRoom(roomName, JoinType.Solo);
        }

        public static string Title { get; private set; } = "Unknown";
        public static string Artist { get; private set; } = "Unknown";
        public static Texture2D Icon { get; private set; } = new Texture2D(2, 2);
        public static bool Paused { get; private set; } = true;
        public static bool ValidData { get; private set; }

        public static float StartTime { get; private set; }
        public static float EndTime { get; private set; }
        public static float ElapsedTime { get; private set; }

        public async Task UpdateDataAsync()
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = quickSongPath,
                Arguments = "-all",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            using Process proc = new Process { StartInfo = psi };
            proc.Start();
            string output = await proc.StandardOutput.ReadToEndAsync();

            await Task.Run(() => proc.WaitForExit());

            ValidData = false;
            Paused = true;
            Title = "Unknown";
            Artist = "Unknown";

            StartTime = 0f;
            EndTime = 0f;
            ElapsedTime = 0f;

            try
            {
                Dictionary<string, object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(output);
                Title = (string)data["Title"];
                Artist = (string)data["Artist"];

                StartTime = Convert.ToSingle(data["StartTime"]);
                EndTime = Convert.ToSingle(data["EndTime"]);
                ElapsedTime = Convert.ToSingle(data["ElapsedTime"]);

                Paused = (string)data["Status"] != "Playing";
                Icon.LoadImage(Convert.FromBase64String((string)data["ThumbnailBase64"]));

                ValidData = true;
            }
            catch { }
        }

        System.Collections.IEnumerator UpdateDataCoroutine(float delay = 0f)
        {
            yield return new WaitForSeconds(delay);

            _ = UpdateDataAsync();
            yield return null;
        }

        // Credits to The-Graze/MusicControls for control methods
        internal enum VirtualKeyCodes
        : uint
        {
            NEXT_TRACK = 0xB0,
            PREVIOUS_TRACK = 0xB1,
            PLAY_PAUSE = 0xB3,
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        internal static extern void keybd_event(uint bVk, uint bScan, uint dwFlags, uint dwExtraInfo);
        internal static void SendKey(VirtualKeyCodes virtualKeyCode) => keybd_event((uint)virtualKeyCode, 0, 0, 0);

        public void PreviousTrack()
        {
            StartCoroutine(UpdateDataCoroutine(0.1f));
            ElapsedTime = 0f;
            SendKey(VirtualKeyCodes.PREVIOUS_TRACK);
        }

        public void PauseTrack()
        {
            Paused = !Paused;
            SendKey(VirtualKeyCodes.PLAY_PAUSE);
        }

        public void SkipTrack()
        {
            StartCoroutine(UpdateDataCoroutine(0.1f));
            ElapsedTime = 0f;
            SendKey(VirtualKeyCodes.NEXT_TRACK);
        }
    }
}
