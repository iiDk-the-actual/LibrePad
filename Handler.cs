using LibrePad.Classes;
using UnityEngine;

namespace LibrePad
{
    public class Handler : MonoBehaviour
    {
        public static Handler Instance { get; private set; }
        void Awake() =>
            Instance = this;

        bool initialized;
        void Update()
        {
            if (!initialized && GorillaLocomotion.GTPlayer.Instance != null && VRRig.LocalRig != null && GorillaTagger.Instance != null)
            {
                initialized = true;
                Tablet.InitializeTablet();
                gameObject.AddComponent<Notifications>();
            }
        }
    }
}
