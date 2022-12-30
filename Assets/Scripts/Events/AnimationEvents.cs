using MathFighter.Scenes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MathFighter.Events
{
    public class AnimationEvents : MonoBehaviour
    {
        [SerializeField]
        public GameObject m_MainMenu;
        [SerializeField]
        public MainScene m_MainScene;
        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        public void OnTitleImageFinished()
        {
            m_MainMenu.SetActive(true);
            m_MainMenu.GetComponent<Animator>().SetTrigger("Enter");
        }

        public void ReleasePending()
        {
            m_MainScene.OnMenuExited();
        }
    }
}