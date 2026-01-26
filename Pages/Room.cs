using GorillaNetworking;
using LibrePad.Utilities;
using Photon.Pun;
using System.Globalization;
using TMPro;
using UnityEngine;

namespace LibrePad.Pages
{
    public class Room : MonoBehaviour
    {
        private TextMeshPro visual;
        private TextMeshPro info;

        public void InitializePage()
        {
            Transform pageTransform = transform;

            visual = pageTransform.Find("Visual").GetComponent<TextMeshPro>();
            info = pageTransform.Find("Info").GetComponent<TextMeshPro>();

            pageTransform.Find("Disconnect").AddComponent<Classes.Button>().OnClick += () => NetworkSystem.Instance.ReturnToSinglePlayer();
            pageTransform.Find("PublicHop").AddComponent<Classes.Button>().OnClick += () => PublicHop();
            pageTransform.Find("PrivateHop").AddComponent<Classes.Button>().OnClick += () => PrivateHop();
        }

        private float updateDelay;
        public void Update()
        {
            if (Time.time > updateDelay)
            {
                updateDelay = Time.time + 1f;

                if (!NetworkSystem.Instance.InRoom)
                {
                    visual.SafeSetText("");
                    info.SafeSetText("Not connected to room");
                }
                else
                {
                    visual.SafeSetText(@"Name
Gamemode
Queue
Players
Public");

                    string gamemode = GorillaGameManager.instance?.GameModeName() ?? "Null";
                    if (gamemode.Contains("Super"))
                        gamemode.Replace("Super", "S.");

                    gamemode = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(gamemode.ToLower());

                    string queue = "Null";

                    PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("gameMode", out object gmObject);
                    string gmString = gmObject.ToString().ToUpper();

                    if (gmString.Contains("DEFAULT"))
                        queue = "Default";
                    else if (gmString.Contains("MINIGAMES"))
                        queue = "Minigames";
                    else if (gmString.Contains("Competitive"))
                        queue = "Competitive";

                    queue = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(queue.ToLower());

                    info.SafeSetText($@"{PhotonNetwork.CurrentRoom?.Name ?? "Null"}
{gamemode}
{queue}
{PhotonNetwork.PlayerList?.Length ?? -1}/{PhotonNetwork.CurrentRoom?.MaxPlayers ?? -1}
{(PhotonNetwork.CurrentRoom?.IsVisible ?? true ? "Public" : "Private")}");
                }
            }
        }

        public void PublicHop()
        {
            GorillaNetworkJoinTrigger trigger = PhotonNetworkController.Instance.currentJoinTrigger ?? GorillaComputer.instance.GetJoinTriggerForZone("forest");
            PhotonNetworkController.Instance.AttemptToJoinPublicRoom(trigger);
        }

        public void PrivateHop()
        {
            string roomName = NetworkSystem.Instance.GetMyNickName().ToUpper();

            if (roomName.Length > 6)
                roomName = roomName[..6];

            roomName += UnityEngine.Random.Range(0, 9999).ToString().PadLeft(4);

            PhotonNetworkController.Instance.AttemptToJoinSpecificRoom(roomName, JoinType.Solo);
        }
    }
}
