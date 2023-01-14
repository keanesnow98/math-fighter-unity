using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MathFighter.Core
{
    [RequireComponent(typeof(AudioSource))]
    public class BackgroundMusic : MonoBehaviour
    {

        private AudioClip currentMusic;

        private static BackgroundMusic backgroundMusic;

        public static BackgroundMusic instance
        {
            get
            {
                if (!backgroundMusic)
                {
                    backgroundMusic = FindObjectOfType(typeof(BackgroundMusic)) as BackgroundMusic;

                    if (!backgroundMusic)
                        Debug.LogError("There needs to be one active BackgroundMusic script on a GameObject in your scene.");
                    else
                    {
                        backgroundMusic.Init();

                        //  Sets this to not be destroyed when reloading scene
                        DontDestroyOnLoad(backgroundMusic);
                    }
                }
                return backgroundMusic;
            }
        }

        void Init()
        {
            gameObject.AddComponent<AudioSource>();
        }

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        public static void Play(string name, float delay = 0.0f)
        {
            instance.currentMusic = Resources.Load<AudioClip>($"Audios/Background/{name}");
            AudioSource audioSource = instance.GetComponent<AudioSource>();
            audioSource.clip = instance.currentMusic;
            audioSource.PlayDelayed(delay);
        }
    }
}