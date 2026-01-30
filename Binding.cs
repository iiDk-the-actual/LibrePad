using GorillaNetworking;
using System.IO;
using UnityEngine;
using Valve.VR;

namespace LibrePad
{
    public class Binding
    {
        private static string platform = "Steam";
        public static string bind = "L Grip";
        private static bool click;

        public const string CFilePath = "C:/Program Files (x86)/Steam/steamapps/common/Gorilla Tag/";
        public const string DFilePath = "D:/SteamLibrary/steamapps/common/Gorilla Tag";
        public static void GetBindFromFile(string path)
        {
            if (!Directory.Exists(path))
            {
                Debug.Log($"Path {path} is invalid.");
                return;
            }
            if (Directory.Exists(path))
            {
                if (!File.Exists(path + "/LibrePadBind.txt"))
                {
                    File.WriteAllText(path + "/LibrePadBind.txt", "L Grip");
                }
                bind = File.ReadAllText(path + "/LibrePadBind.txt");
            }
        }

        public static bool Control(string _bind)
        {
            if (PlayFabAuthenticator.instance.platform.PlatformTag != "Steam")
                platform = "PC";
            else
                platform = "Steam";
            switch (_bind)
            {
                case "L Grip": return ControllerInputPoller.instance.leftGrab;
                case "R Grip": return ControllerInputPoller.instance.rightGrab;
                case "L Trigger": return ControllerInputPoller.instance.leftControllerTriggerButton;
                case "R Trigger": return ControllerInputPoller.instance.rightControllerTriggerButton;
                case "L Primary": return ControllerInputPoller.instance.leftControllerPrimaryButton;
                case "R Primary": return ControllerInputPoller.instance.rightControllerPrimaryButton;
                case "L Secondary": return ControllerInputPoller.instance.leftControllerSecondaryButton;
                case "R Secondary": return ControllerInputPoller.instance.rightControllerSecondaryButton;
                case "L Joystick": return platform == "Steam" ? SteamVR_Actions.gorillaTag_LeftJoystickClick.GetState(SteamVR_Input_Sources.LeftHand) : ControllerInputPoller.instance.leftControllerDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxisClick, out click);
                case "R Joystick": return platform == "Steam" ? SteamVR_Actions.gorillaTag_RightJoystickClick.GetState(SteamVR_Input_Sources.RightHand) : ControllerInputPoller.instance.rightControllerDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxisClick, out click);
                default: return ControllerInputPoller.instance.leftGrab;
            }
        }
    }
}