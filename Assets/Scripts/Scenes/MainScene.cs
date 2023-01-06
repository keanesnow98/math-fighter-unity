using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

using MathFighter.Core;

namespace MathFighter.Scenes
{
    public class MainScene : MonoBehaviour
    {
        private Animator previousMenu;
        private Animator currentPendingMenu;
        private string currentPendingScene;
        private GameObject currentPendingDialog;

        [SerializeField]
        public BackgroundMusic m_BackgroundMusic;
        [SerializeField]
        public GameObject m_StartButton;

        [SerializeField]
        public Animator m_TitleImage;

        [SerializeField]
        public Animator m_MainMenu;

        [SerializeField]
        public Animator m_Highscores;

        [SerializeField]
        public Transform m_Dialogs;

        [SerializeField]
        public GameObject m_BackButton;

        // Start is called before the first frame update
        void Start()
        {
            Instantiate<BackgroundMusic>(m_BackgroundMusic);
        }

        // Update is called once per frame
        void Update()
        {
        }

        public void OnStartButtonClicked()
        {
            m_TitleImage.SetBool("Started", true);
            m_StartButton.SetActive(false);
            BackgroundMusic.Play("Intro", 1.0f);
        }

        public void OnTitleImageExited()
        {
            m_MainMenu.GetComponent<Animator>().SetTrigger("Start");
        }

        public void OnMenuClicked(Animator menu)
        {
            m_MainMenu.SetTrigger("Exit");

            previousMenu = m_MainMenu;
            currentPendingDialog = null;
            currentPendingScene = null;
            currentPendingMenu = menu;
        }

        public void OnHighscoreClicked(GameObject highscoresDialog)
        {
            m_Highscores.SetTrigger("Exit");

            previousMenu = m_MainMenu;
            currentPendingDialog = highscoresDialog;
            currentPendingScene = null;
            currentPendingMenu = m_Highscores;
        }

        public void OnHowToPlayClicked(GameObject helpDialog)
        {
            m_MainMenu.SetTrigger("Exit");

            previousMenu = m_MainMenu;
            currentPendingDialog = helpDialog;
            currentPendingScene = null;
            currentPendingMenu = m_MainMenu;
        }

        public void OnMenuExited()
        {
            if (currentPendingScene != null)
                SceneManager.LoadScene(currentPendingScene, LoadSceneMode.Additive);
            else if (currentPendingDialog != null)
            {
                currentPendingDialog = Instantiate<GameObject>(currentPendingDialog, m_Dialogs);
                previousMenu = currentPendingMenu;
                m_BackButton.SetActive(true);
            }
            else
            {
                previousMenu.gameObject.SetActive(false);
                currentPendingMenu.gameObject.SetActive(true);
                currentPendingMenu.SetTrigger("Start");
            }
        }

        public void OnBackToMenuClicked(Animator subMenu)
        {
            subMenu.SetTrigger("Exit");

            previousMenu = subMenu;
            currentPendingScene = null;
            currentPendingMenu = m_MainMenu;
        }

        public void OnQuitButtonClicked()
        {
            Application.Quit();
        }

        public void OnBackButtonClicked()
        {
            Destroy(currentPendingDialog.gameObject);
            m_BackButton.SetActive(false);
            previousMenu.SetTrigger("Start");
            previousMenu = null;
        }
    }
}
