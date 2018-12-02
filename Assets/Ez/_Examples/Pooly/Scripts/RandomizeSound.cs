// Copyright (c) 2016 - 2017 Ez Entertainment SRL. All Rights Reserved.
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms

using UnityEngine;
using System.Collections;

namespace Ez.Pooly.Examples
{
    [RequireComponent(typeof(AudioSource))]
    public class RandomizeSound : MonoBehaviour
    {
        public AudioSource aSource;
        public float minVolume = 0.6f;
        public float maxVolume = 1f;
        public float minPitch = 0.9f;
        public float maxPitch = 1.1f;
        [Space(20)]
        public bool setRandomSound = false;
        public AudioClip[] sounds;
        public bool delayPlaySound = false;
        public float playSoundDelay = 0.6f;

        void Start()
        {
            if (aSource == null) { aSource = GetComponent<AudioSource>() == null ? gameObject.AddComponent<AudioSource>() : GetComponent<AudioSource>(); }
            aSource.playOnAwake = !delayPlaySound;
            aSource.volume = Random.Range(minVolume, maxVolume);
            aSource.pitch = Random.Range(minPitch, maxPitch);
            if (!setRandomSound || sounds == null || sounds.Length == 0) { return; }
            aSource.clip = sounds[Random.Range(0, sounds.Length - 1)];
        }

        void OnSpawned()
        {
            if (aSource == null) { return; }
            if (delayPlaySound) { StartCoroutine("iPlaySound"); }
        }

        IEnumerator iPlaySound()
        {
            yield return new WaitForSeconds(playSoundDelay);
            aSource.Play();
        }

    }
}
