using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MathFighter.Core
{
    [RequireComponent(typeof(Button))]
    public class PlaySound : MonoBehaviour
    {
        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        public void Play(string name)
        {
            AudioClip audioClip = Resources.Load<AudioClip>($"Audios/UI/{name}");
            AudioSource audioSource = new GameObject("Play Sound").AddComponent<AudioSource>();
            audioSource.clip = audioClip;
            audioSource.volume = PlayerPrefs.GetInt("sounds_volume", 100) * 0.01f;
            audioSource.Play();
            Destroy(audioSource.gameObject, audioClip.length);
        }
    }
}