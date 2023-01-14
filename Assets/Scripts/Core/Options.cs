using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MathFighter.Core
{
    public class Options : MonoBehaviour
    {
        private int musicVolume;
        private int soundsVolume;
        private AudioSource m_BackgroundMusic;

        [SerializeField]
        public Text m_MusicText;
        [SerializeField]
        public Text m_SoundsText;
        // Start is called before the first frame update
        void Start()
        {
            musicVolume = PlayerPrefs.GetInt("music_volume", 100);
            soundsVolume = PlayerPrefs.GetInt("sounds_volume", 100);

            m_BackgroundMusic = BackgroundMusic.instance.GetComponent<AudioSource>();
            m_BackgroundMusic.volume = musicVolume * 0.01f;
            m_MusicText.text = $"Music: {musicVolume}%";
            m_SoundsText.text = $"Sounds: {soundsVolume}%";
        }

        // Update is called once per frame
        void Update()
        {

        }

        public void OnReduceMusic()
        {
            if (musicVolume > 0)
            {
                musicVolume -= 10;
                PlayerPrefs.SetInt("music_volume", musicVolume);
                m_BackgroundMusic.volume = musicVolume * 0.01f;
                m_MusicText.text = $"Music: {musicVolume}%";
            }
        }

        public void OnIncreaseMusic()
        {
            if (musicVolume < 100)
            {
                musicVolume += 10;
                PlayerPrefs.SetInt("music_volume", musicVolume);
                m_BackgroundMusic.volume = musicVolume * 0.01f;
                m_MusicText.text = $"Music: {musicVolume}%";
            }
        }

        public void OnReduceSounds()
        {
            if (soundsVolume > 0)
            {
                soundsVolume -= 10;
                PlayerPrefs.SetInt("sounds_volume", soundsVolume);
                m_SoundsText.text = $"Music: {soundsVolume}%";
            }
        }

        public void OnIncreaseSounds()
        {
            if (soundsVolume < 100)
            {
                soundsVolume += 10;
                PlayerPrefs.SetInt("sounds_volume", soundsVolume);
                m_SoundsText.text = $"Music: {soundsVolume}%";
            }
        }
    }
}