using GorillaExtensions;
using GorillaLocomotion;
using GorillaNetworking;
using LibrePad.Classes;
using LibrePad.Patches;
using LibrePad.Utilities;
using Photon.Pun;
using PlayFab;
using PlayFab.ClientModels;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace LibrePad.Pages
{
    public class Player : MonoBehaviour
    {
        private Camera camera;

        private GameObject photo;
        private TextMeshPro visual;
        private TextMeshPro info;

        private GameObject usage;
        private GameObject mods;

        public void InitializePage()
        {
            Transform pageTransform = transform;

            photo = pageTransform.Find("Photo").gameObject;

            visual = pageTransform.Find("Visual").GetComponent<TextMeshPro>();
            info = pageTransform.Find("Info").GetComponent<TextMeshPro>();

            GameObject cameraObject = new GameObject("LibrePad_SpectateCamera");
            RenderTexture renderTexture = new RenderTexture(512, 512, 16);
            camera = cameraObject.AddComponent<Camera>();
            camera.targetTexture = renderTexture;
            cameraObject.transform.localPosition = new Vector3(0f, 0f, 1f);
            cameraObject.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            
            photo.GetComponent<Renderer>().material = new Material(Shader.Find("Universal Render Pipeline/Unlit"))
            {
                mainTexture = renderTexture
            };

            Transform usageTransform = pageTransform.Find("Usage");
            Transform modsTransform = pageTransform.Find("Mods");

            usage = usageTransform.gameObject;
            mods = modsTransform.gameObject;

            usageTransform.Find("Mute").AddComponent<Button>().OnClick += () =>
            {
                GorillaPlayerScoreboardLine scoreboardLine = GorillaScoreboardTotalUpdater.allScoreboardLines.FirstOrDefault(line => line.linePlayer == targetRig.OwningNetPlayer);

                scoreboardLine.muteButton.isOn = !scoreboardLine.muteButton.isOn;
                scoreboardLine?.PressButton(scoreboardLine.muteButton.isOn, GorillaPlayerLineButton.ButtonType.Mute);

                usageTransform.Find("Mute/Text").GetComponent<TextMeshPro>().SafeSetText(scoreboardLine.muteButton.isOn ? "Unmute" : "Mute");
            };

            usageTransform.Find("Report").AddComponent<Button>().OnClick += () =>
            {
                usageTransform.Find("Report").gameObject.SetActive(false);
                usageTransform.Find("ReportReasons").gameObject.SetActive(true);
            };

            usageTransform.Find("ReportReasons/Cheating").AddComponent<Button>().OnClick += () =>
            {
                GorillaPlayerScoreboardLine scoreboardLine = GorillaScoreboardTotalUpdater.allScoreboardLines.FirstOrDefault(line => line.linePlayer == targetRig.OwningNetPlayer);
                scoreboardLine?.PressButton(true, GorillaPlayerLineButton.ButtonType.Cheating);

                usageTransform.Find("Report").gameObject.SetActive(true);
                usageTransform.Find("ReportReasons").gameObject.SetActive(false);
            };

            usageTransform.Find("ReportReasons/Toxicity").AddComponent<Button>().OnClick += () =>
            {
                GorillaPlayerScoreboardLine scoreboardLine = GorillaScoreboardTotalUpdater.allScoreboardLines.FirstOrDefault(line => line.linePlayer == targetRig.OwningNetPlayer);
                scoreboardLine?.PressButton(true, GorillaPlayerLineButton.ButtonType.Toxicity);

                usageTransform.Find("Report").gameObject.SetActive(true);
                usageTransform.Find("ReportReasons").gameObject.SetActive(false);
            };

            usageTransform.Find("ReportReasons/HateSpeech").AddComponent<Button>().OnClick += () =>
            {
                GorillaPlayerScoreboardLine scoreboardLine = GorillaScoreboardTotalUpdater.allScoreboardLines.FirstOrDefault(line => line.linePlayer == targetRig.OwningNetPlayer);
                scoreboardLine?.PressButton(true, GorillaPlayerLineButton.ButtonType.HateSpeech);

                usageTransform.Find("Report").gameObject.SetActive(true);
                usageTransform.Find("ReportReasons").gameObject.SetActive(false);
            };

            usageTransform.Find("ReportReasons/Cancel").AddComponent<Button>().OnClick += () =>
            {
                usageTransform.Find("Report").gameObject.SetActive(true);
                usageTransform.Find("ReportReasons").gameObject.SetActive(false);
            };

            usageTransform.Find("Prioritize").AddComponent<Button>().OnClick += () =>
            {
                PrioritizeVoicePatch.prioritizedRig = PrioritizeVoicePatch.prioritizedRig == targetRig ? null : targetRig;
                usageTransform.Find("Prioritize/Text").GetComponent<TextMeshPro>().SafeSetText(PrioritizeVoicePatch.prioritizedRig == targetRig ? "Unprioritize" : "Prioritize");
            };

            usageTransform.Find("Mods").AddComponent<Button>().OnClick += () =>
            {
                usage.SetActive(false);
                mods.SetActive(true);

                updateTime = 0f;
            };

            modsTransform.Find("Exit").AddComponent<Button>().OnClick += () =>
            {
                usage.SetActive(true);
                mods.SetActive(false);
            };
        }

        private VRRig targetRig;
        private float updateTime;
        private GameObject selectObject;

        private VRRig lastTarget;
        private bool lastTriggerSelect;
        private AudioClip selectSound;

        public void Update()
        {
            bool canSelect = NetworkSystem.Instance.InRoom && Vector3.Distance(transform.position, GorillaTagger.Instance.rightHandTransform.position) > 0.4f;
            if (canSelect)
            {
                if (selectObject == null)
                    selectObject = new GameObject("PingLine");

                Color targetColor = Tablet.Instance.backgroundMaterial.color;
                Color lineColor = targetColor;
                lineColor.a = 0.15f;

                LineRenderer pingLine = selectObject.GetOrAddComponent<LineRenderer>();
                pingLine.material.shader = Shader.Find("GUI/Text Shader");
                pingLine.startColor = lineColor;
                pingLine.endColor = lineColor;
                pingLine.startWidth = 0.025f * GTPlayer.Instance.scale;
                pingLine.endWidth = 0.025f * GTPlayer.Instance.scale;
                pingLine.positionCount = 2;
                pingLine.useWorldSpace = true;

                pingLine.numCapVertices = 10;
                pingLine.numCornerVertices = 5;

                var (_, _, _, forward, _) = GetTrueHandPosition(false);

                Vector3 StartPosition = GorillaTagger.Instance.rightHandTransform.position;
                Vector3 Direction = forward;

                Physics.SphereCast(StartPosition + Direction / 4f * GTPlayer.Instance.scale, 0.15f, Direction, out var Ray, 512f, NoInvisLayerMask());
                Vector3 EndPosition = Ray.point == Vector3.zero ? StartPosition + (Direction * 512f) : Ray.point;

                pingLine.SetPosition(0, StartPosition);
                pingLine.SetPosition(1, EndPosition);

                VRRig rigTarget = Ray.collider.GetComponentInParent<VRRig>();
                if (Ray.collider != null && rigTarget != null && !rigTarget.isLocal)
                {
                    if (lastTarget != null && lastTarget != rigTarget)
                    {
                        lastTarget.mainSkin.material.shader = Shader.Find("GorillaTag/UberShader");
                        if (lastTarget.mainSkin.material.name.Contains("gorilla_body"))
                            lastTarget.mainSkin.material.color = lastTarget.playerColor;

                        lastTarget = null;
                    }

                    if (lastTarget == null)
                    {
                        FixRigMaterial(rigTarget);

                        rigTarget.mainSkin.material.shader = Shader.Find("GUI/Text Shader");
                        rigTarget.mainSkin.material.color = targetColor;

                        GorillaTagger.Instance.StartVibration(false, GorillaTagger.Instance.tagHapticStrength / 2f, 0.05f);

                        lastTarget = rigTarget;
                    }
                    else
                        lastTarget.mainSkin.material.color = targetColor;

                    bool trigger = ControllerInputPoller.TriggerFloat(UnityEngine.XR.XRNode.RightHand) > 0.5f;

                    if (trigger && !lastTriggerSelect)
                    {
                        GorillaTagger.Instance.StartVibration(false, GorillaTagger.Instance.tagHapticStrength / 2f, GorillaTagger.Instance.tagHapticDuration / 2f);

                        selectSound ??= Utilities.Assets.LoadAsset<AudioClip>("select");
                        AudioSource audioSource = VRRig.LocalRig.rightHandPlayer;
                        audioSource.volume = 0.3f;
                        audioSource.PlayOneShot(selectSound);

                        updateTime = 0f;
                        targetRig = rigTarget;
                    }

                    lastTriggerSelect = trigger;
                }
                else
                {
                    if (lastTarget != null)
                    {
                        lastTarget.mainSkin.material.shader = Shader.Find("GorillaTag/UberShader");
                        if (lastTarget.mainSkin.material.name.Contains("gorilla_body"))
                            lastTarget.mainSkin.material.color = lastTarget.playerColor;

                        lastTarget = null;
                    }
                }
            }
            else
            {
                if (selectObject != null)
                {
                    Destroy(selectObject);
                    selectObject = null;
                }

                if (lastTarget != null)
                {
                    lastTarget.mainSkin.material.shader = Shader.Find("GorillaTag/UberShader");
                    if (lastTarget.mainSkin.material.name.Contains("gorilla_body"))
                        lastTarget.mainSkin.material.color = lastTarget.playerColor;

                    lastTarget = null;
                }

                lastTriggerSelect = false;
            }

            if (Time.time > updateTime || !targetRig.Active())
            {
                updateTime = Time.time + 0.5f;

                if (targetRig.Active())
                {
                    camera.gameObject.transform.SetParent(targetRig.headMesh.transform, false);
                    photo.SetActive(true);

                    if ((usage.activeSelf && mods.activeSelf) || (!usage.activeSelf && !mods.activeSelf))
                    {
                        usage.SetActive(true);
                        mods.SetActive(false);
                    }

                    visual.SafeSetText($@"Name
Color
Platform
FPS
Ping
Creation Date");
                    info.SafeSetText($@"{targetRig.playerNameVisible}
{GetColor(targetRig)}
{GetPlatform(targetRig)}
{targetRig.fps}
{GetPing(targetRig)}
{GetCreationDate(targetRig.OwningNetPlayer.UserId, (str) => updateTime = 0f)}");

                    List<string> legalMods = new List<string>();
                    List<string> illegalMods = new List<string>();

                    Dictionary<string, object> customProps = new Dictionary<string, object>();
                    foreach (DictionaryEntry dictionaryEntry in targetRig.OwningNetPlayer.GetPlayerRef().CustomProperties)
                        customProps[dictionaryEntry.Key.ToString().ToLower()] = dictionaryEntry.Value;

                    foreach (var mod in modDictionary.Where(mod => customProps.ContainsKey(mod.Key.ToLower())))
                    {
                        if (mod.Value.legal)
                            legalMods.Add(mod.Value.name);
                        else
                            illegalMods.Add(mod.Value.name);
                    }

                    CosmeticsController.CosmeticSet cosmeticSet = targetRig.cosmeticSet;
                    if (cosmeticSet.items.Any(cosmetic => !cosmetic.isNullItem && !targetRig.rawCosmeticString.Contains(cosmetic.itemName)))
                        illegalMods.Add("Cosmetx");

                    if (usage.activeSelf)
                        usage.transform.Find("Mods/Text").GetComponent<TextMeshPro>().SafeSetText($"Mods (<color=green>{legalMods.Count}</color>:<color=red>{illegalMods.Count}</color>)");
                    else if (mods.activeSelf)
                    {
                        mods.transform.Find("Legal").GetComponent<TextMeshPro>().SafeSetText(string.Join("\n", legalMods));
                        mods.transform.Find("Illegal").GetComponent<TextMeshPro>().SafeSetText(string.Join("\n", illegalMods));
                    }
                }
                else
                {
                    targetRig = null;

                    photo.SetActive(false);
                    usage.SetActive(false);
                    mods.SetActive(false);

                    visual.SafeSetText("");
                    info.SafeSetText("No player selected\nUse your right hand to select players");
                }
            }
        }

        public void OnDisable()
        {
            if (selectObject != null)
            {
                Destroy(selectObject);
                selectObject = null;
            }

            if (lastTarget != null)
            {
                lastTarget.mainSkin.material.shader = Shader.Find("GorillaTag/UberShader");
                if (lastTarget.mainSkin.material.name.Contains("gorilla_body"))
                    lastTarget.mainSkin.material.color = lastTarget.playerColor;

                lastTarget = null;
            }
        }

        private static int? noInvisLayerMask;
        public static int NoInvisLayerMask()
        {
            noInvisLayerMask ??= ~(
                1 << LayerMask.NameToLayer("TransparentFX") |
                1 << LayerMask.NameToLayer("Ignore Raycast") |
                1 << LayerMask.NameToLayer("Zone") |
                1 << LayerMask.NameToLayer("Gorilla Trigger") |
                1 << LayerMask.NameToLayer("Gorilla Boundary") |
                1 << LayerMask.NameToLayer("GorillaCosmetics") |
                1 << LayerMask.NameToLayer("GorillaParticle"));

            return noInvisLayerMask ?? GTPlayer.Instance.locomotionEnabledLayers;
        }

        public static (Vector3 position, Quaternion rotation, Vector3 up, Vector3 forward, Vector3 right) GetTrueHandPosition(bool left)
        {
            Transform controllerTransform = left ? GorillaTagger.Instance.leftHandTransform : GorillaTagger.Instance.rightHandTransform;
            GTPlayer.HandState handState = left ? GTPlayer.Instance.LeftHand : GTPlayer.Instance.RightHand;

            Quaternion rot = controllerTransform.rotation * handState.handRotOffset;
            return (controllerTransform.position + controllerTransform.rotation * (handState.handOffset * GTPlayer.Instance.scale), rot, rot * Vector3.up, rot * Vector3.forward, rot * Vector3.right);
        }

        private static readonly List<VRRig> convertedRigs = new List<VRRig>();
        public static void FixRigMaterial(VRRig rig)
        {
            if (!convertedRigs.Contains(rig))
            {
                convertedRigs.Add(rig);

                rig.mainSkin.sharedMesh.colors32 = Enumerable.Repeat((Color32)Color.white, rig.mainSkin.sharedMesh.colors32.Length).ToArray();
                rig.mainSkin.sharedMesh.colors = Enumerable.Repeat(Color.white, rig.mainSkin.sharedMesh.colors.Length).ToArray();
            }
        }

        public static string GetPlatform(VRRig rig)
        {
            string concatStringOfCosmeticsAllowed = rig.rawCosmeticString;

            if (concatStringOfCosmeticsAllowed.Contains("S. FIRST LOGIN"))
                return "Steam";
            else if (concatStringOfCosmeticsAllowed.Contains("FIRST LOGIN") || rig.Creator.GetPlayerRef().CustomProperties.Count >= 2)
                return "PC";

            return "Standalone";
        }

        // Thanks HanSolo1000Falcon for patchless implementation
        public static int GetPing(VRRig rig)
        {
            try
            {
                CircularBuffer<VRRig.VelocityTime> history = rig.velocityHistoryList;
                if (history != null && history.Count > 0)
                {
                    double ping = Math.Abs((history[0].time - PhotonNetwork.Time) * 1000);

                    return (int)Math.Clamp(Math.Round(ping), 0, int.MaxValue);
                }
            }
            catch
            {
            }

            return int.MaxValue;
        }

        public static string GetColor(VRRig rig) =>
            string.Format("{0}, {1}, {2}", Mathf.RoundToInt(rig.playerColor.r * 9f), Mathf.RoundToInt(rig.playerColor.g * 9f), Mathf.RoundToInt(rig.playerColor.b * 9f));

        public static readonly Dictionary<string, float> waitingForCreationDate = new Dictionary<string, float>();
        public static readonly Dictionary<string, string> creationDateCache = new Dictionary<string, string>();
        public static string GetCreationDate(string input, Action<string> onTranslated = null, string format = "MM/dd/yyyy")
        {
            if (creationDateCache.TryGetValue(input, out string date))
                return date;
            if (!waitingForCreationDate.ContainsKey(input))
            {
                waitingForCreationDate[input] = Time.time + 10f;
                GetCreationCoroutine(input, onTranslated, format);
            }
            else
            {
                if (!(Time.time > waitingForCreationDate[input])) return "Loading...";
                waitingForCreationDate[input] = Time.time + 10f;
                GetCreationCoroutine(input, onTranslated, format);
            }

            return "Loading...";
        }

        public static void GetCreationCoroutine(string userId, Action<string> onTranslated = null, string format = "MM/dd/yyyy")
        {
            if (creationDateCache.TryGetValue(userId, out string date))
            {
                onTranslated?.Invoke(date);
                return;
            }

            PlayFabClientAPI.GetAccountInfo(new GetAccountInfoRequest { PlayFabId = userId }, delegate (GetAccountInfoResult result) // Who designed this
            {
                string creationDate = result.AccountInfo.Created.ToString(format);
                creationDateCache[userId] = creationDate;

                onTranslated?.Invoke(creationDate);
            }, delegate { creationDateCache[userId] = "Null"; onTranslated?.Invoke("Null"); });
        }

        public struct ModInfo
        {
            public string name; public bool legal;
        }

        public static readonly Dictionary<string, ModInfo> modDictionary = new Dictionary<string, ModInfo> {
            { "genesis", new ModInfo { name = "Genesis", legal = false } },
            { "HP_Left", new ModInfo { name = "Holdable Pad", legal = true } },
            { "GrateVersion", new ModInfo { name = "Grate", legal = true } },
            { "void", new ModInfo { name = "Void", legal = false } },
            { "BANANAOS", new ModInfo { name = "Banana OS", legal = true } },
            { "GC", new ModInfo { name = "GorillaCraft", legal = true } },
            { "CarName", new ModInfo { name = "GorillaVehicles", legal = true } },
            { "6p72ly3j85pau2g9mda6ib8px", new ModInfo { name = "ColossalCheatMenu V2", legal = false } },
            { "6XpyykmrCthKhFeUfkYGxv7xnXpoe2", new ModInfo { name = "ColossalCheatMenu V2", legal = false } },
            { "FPS-Nametags for Zlothy", new ModInfo { name = "Zlothy FPS Tags", legal = true } },
            { "cronos", new ModInfo { name = "Cronos", legal = false } },
            { "ORBIT", new ModInfo { name = "Orbit", legal = false } },
            { "Violet On Top", new ModInfo { name = "Violet", legal = false } },
            { "violetpaiduser", new ModInfo { name = "Violet Paid", legal = false } },
            { "violetfree", new ModInfo { name = "Violet Free", legal = false } },
            { "MP25", new ModInfo { name = "MonkePhone", legal = true } },
            { "monkephone", new ModInfo { name = "MonkePhone", legal = true } },
            { "GorillaWatch", new ModInfo { name = "GorillaWatch", legal = true } },
            { "InfoWatch", new ModInfo { name = "GorillaInfoWatch", legal = true } },
            { "BananaPhone", new ModInfo { name = "Banana Phone", legal = true } },
            { "Vivid", new ModInfo { name = "Vivid", legal = false } },
            { "RGBA", new ModInfo { name = "Custom Cosmetics", legal = true } },
            { "colour", new ModInfo { name = "Custom Cosmetics", legal = true } },
            { "cheese is gouda", new ModInfo { name = "WhoIsCheating", legal = true } },
            { "shirtversion", new ModInfo { name = "GorillaShirts", legal = true } },
            { "gpronouns", new ModInfo { name = "GorillaPronouns", legal = true } },
            { "gfaces", new ModInfo { name = "GorillaFaces", legal = true } },
            { "pmversion", new ModInfo { name = "PlayerModels", legal = true } },
            { "gtrials", new ModInfo { name = "GorillaTrial", legal = true } },
            { "msp", new ModInfo { name = "MonkeSmartphone", legal = true } },
            { "gorillastats", new ModInfo { name = "GorillaStats", legal = true } },
            { "using gorilladrift", new ModInfo { name = "GorillaDrift", legal = true } },
            { "monkehavocversion", new ModInfo { name = "MonkeHavoc", legal = true } },
            { "tictactoe", new ModInfo { name = "TicTacToe", legal = true } },
            { "ccolor", new ModInfo { name = "Index", legal = true } },
            { "imposter", new ModInfo { name = "GorillaAmongUs", legal = true } },
            { "spectapeversion", new ModInfo { name = "Spectape", legal = true } },
            { "cats", new ModInfo { name = "Cats", legal = false } },
            { "made by biotest05 :3", new ModInfo { name = "Dogs", legal = false } },
            { "fys cool magic mod", new ModInfo { name = "FYSMagicMod", legal = false } },
            { "chainedtogether", new ModInfo { name = "Chained Together", legal = true } },
            { "ChainedTogetherActive", new ModInfo { name = "Chained Together", legal = true } },
            { "goofywalkversion", new ModInfo { name = "Goofy Walk", legal = true } },
            { "void_menu_open", new ModInfo { name = "Void", legal = false } },
            { "obsidianmc", new ModInfo { name = "Obsidian.lol", legal = false } },
            { "dark", new ModInfo { name = "ShibaGT Dark", legal = false } },
            { "hidden", new ModInfo { name = "Hidden Menu", legal = false } },
            { "oblivionuser", new ModInfo { name = "Oblivion", legal = false } },
            { "hgrehngio889584739_hugb\n", new ModInfo { name = "Resurgence", legal = false } },
            { "hgrehngio889584739_hugb", new ModInfo { name = "Resurgence", legal = false } },
            { "eyerock reborn", new ModInfo { name = "Eyerock Reborn", legal = false } },
            { "asteroidlite", new ModInfo { name = "Asteroid Lite", legal = false } },
            { "elux", new ModInfo { name = "Elux", legal = false } },
            { "cokecosmetics", new ModInfo { name = "Coke Cosmetx", legal = false } },
            { "GFaces", new ModInfo { name = "G Faces", legal = false } },
            { "github.com/maroon-shadow/SimpleBoards", new ModInfo { name = "Simple Boards", legal = true } },
            { "github.com/ZlothY29IQ/GorillaMediaDisplay", new ModInfo { name = "Gorilla Media Display", legal = true } },
            { "github.com/ZlothY29IQ/TooMuchInfo", new ModInfo { name = "Too Much Info", legal = false } },
            { "github.com/ZlothY29IQ/RoomUtils-IW", new ModInfo { name = "Room Utils IW", legal = true } },
            { "github.com/ZlothY29IQ/MonkeClick", new ModInfo { name = "Monke Click", legal = true } },
            { "github.com/ZlothY29IQ/MonkeClick-CI", new ModInfo { name = "Monke Click CI", legal = true } },
            { "github.com/ZlothY29IQ/MonkeRealism", new ModInfo { name = "Monke Realism", legal = true } },
            { "github.com/ZlothY29IQ/Zloth-RecRoomRig", new ModInfo { name = "Zloth Rec Room Rig", legal = true } },
            { "MediaPad", new ModInfo { name = "Media Pad", legal = true } },
            { "GorillaCinema", new ModInfo { name = "Gorilla Cinema", legal = true } },
            { "CSVersion", new ModInfo { name = "Custom Skin", legal = true } },
            { "ShirtProperties", new ModInfo { name = "GorillaShirts Legacy", legal = true } },
            { "GS", new ModInfo { name = "GorillaShirts Legacy", legal = true } },
            { "Body Tracking", new ModInfo { name = "Body Track Old", legal = true } },
            { "Body Estimation", new ModInfo { name = "Han Body Est", legal = true } },
            { "Gorilla Track", new ModInfo { name = "Body Track", legal = true } },
            { "CustomMaterial", new ModInfo { name = "Custom Cosmetics", legal = true } },
            { "I like cheese", new ModInfo { name = "Rec Room Rig", legal = true } },
            { "silliness", new ModInfo { name = "Silliness", legal = false } },
            { "EmoteWheel", new ModInfo { name = "Fortnite Emote Wheel", legal = false } },
            { "untitled", new ModInfo { name = "Untitled", legal = false } },
            { "BoyDoILoveInformation Public", new ModInfo { name = "BoyDoILoveInformation", legal = true } },
            { "DTAOI", new ModInfo { name = "DTAOI", legal = false } },
            { "DTASLOI", new ModInfo { name = "DTASLOI", legal = false } },
            { "GorillaShop", new ModInfo { name = "GorillaShop", legal = false } },
            { "Fusioned", new ModInfo { name = "Fusioned", legal = false } },
            { "y u lookin in here weirdo", new ModInfo { name = "Malachi Menu Reborn", legal = false } },
            { "ØƦƁƖƬ", new ModInfo { name = "Orbit", legal = false } },
            { "Atlas", new ModInfo { name = "Atlas", legal = false } }
        };
    }
}
