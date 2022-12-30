using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MathFighter.Scenes
{
    public class MainScene : MonoBehaviour
    {
        private Animator currentPendingMenu;
        private string currentPendingScene;

        [SerializeField]
        public GameObject m_StartButton;
        [SerializeField]
        public Animator m_TitleImage;
        [SerializeField]
        public Animator m_MainMenu;
        [SerializeField]
        public Animator m_Highscores;
        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        public void OnStartButtonClicked()
        {
            m_TitleImage.SetBool("Started", true);
            m_StartButton.SetActive(false);
        }

        public void OnMenuClicked(Animator menu)
        {
            m_MainMenu.SetTrigger("Exit");

            currentPendingScene = null;
            currentPendingMenu = menu;
        }

        public void OnMenuClicked(string scene)
        {
            m_Highscores.SetTrigger("Exit");

            currentPendingScene = scene;
            currentPendingMenu = null;
        }

        public void OnMenuExited()
        {
            if (currentPendingScene != null)
                SceneManager.LoadScene(currentPendingScene, LoadSceneMode.Additive);
            else if (currentPendingMenu != null)
                currentPendingMenu.SetTrigger("Start");
        }
    }
}