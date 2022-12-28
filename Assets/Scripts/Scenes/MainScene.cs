using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MathFighter.Scenes
{
    public class MainScene : MonoBehaviour
    {
        [SerializeField]
        public GameObject m_StartButton;
        [SerializeField]
        public Animator m_TitleImage;
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
    }
}