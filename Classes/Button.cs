using System;
using UnityEngine;

namespace LibrePad.Classes
{
    public class Button : MonoBehaviour
    {
        public event Action OnClick;

        public void Start()
        {
            gameObject.layer = 18;
            gameObject.GetComponent<Collider>().isTrigger = true;
        }

        private static float buttonDelay;
        private static AudioClip buttonSound;

        public void OnTriggerEnter(Collider collider)
        {
            if (collider != null && collider.name == "RightHandTriggerCollider" && Time.time > buttonDelay)
            {
                buttonDelay = Time.time + 0.2f;
                GorillaTagger.Instance.StartVibration(false, GorillaTagger.Instance.tagHapticStrength / 2f, GorillaTagger.Instance.tagHapticDuration / 2f);

                buttonSound ??= Utilities.Assets.LoadAsset<AudioClip>("click");
                AudioSource audioSource = VRRig.LocalRig.rightHandPlayer;
                audioSource.volume = 0.3f;
                audioSource.PlayOneShot(buttonSound);

                OnClick?.Invoke();
            }
        }
    }
}
