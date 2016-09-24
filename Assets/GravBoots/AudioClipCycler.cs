using System;
using UnityEngine;
using UnityStandardAssets.Utility;
using Random = UnityEngine.Random;

namespace GravBoots
{   

    [System.Serializable]
    public class AudioClipCycler
    {

        [SerializeField] private AudioClip[] m_sounds;    // an array of footstep sounds that will be randomly selected from.
        [SerializeField] private float m_volume = 1f;    


        public void PlayNext(AudioSource source) {
            PlayNext (source, m_volume);
        }
            
        public void PlayNext(AudioSource source, float volume) {      
            // pick & play a random sound from the array,
            // excluding sound at index 0
            int n = Random.Range(1, m_sounds.Length);
            source.clip = m_sounds[n];
            source.PlayOneShot(source.clip, volume);
            // move picked sound to index 0 so it's not picked next time
            m_sounds[n] = m_sounds[0];
            m_sounds[0] = source.clip;
        }

    }
}

