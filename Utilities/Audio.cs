using UnityEngine;

namespace LibrePad.Utilities
{
    public class Audio
    {
        public static GameObject audioManager;

        /// <summary>
        /// Plays a 2D audio clip at the specified volume using a singleton audio manager.
        /// </summary>
        /// <param name="sound">The audio clip to play.</param>
        /// <param name="volume">The volume at which to play the audio clip. Defaults to 1f.</param>
        public static void Play2DAudio(AudioClip sound, float volume = 1f)
        {
            if (audioManager == null)
            {
                audioManager = new GameObject("2DAudioMgr");
                AudioSource temp = audioManager.AddComponent<AudioSource>();
                temp.spatialBlend = 0f;
            }
            AudioSource ausrc = audioManager.GetComponent<AudioSource>();
            ausrc.volume = volume;
            ausrc.PlayOneShot(sound);
        }
    }
}