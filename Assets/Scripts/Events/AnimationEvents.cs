using System.Collections;
using System.Collections.Generic;
using MathFighter.Scenes;
using UnityEngine;

namespace MathFighter.Events
{
    public class AnimationEvents : MonoBehaviour
    {
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
            m_MainScene.OnTitleImageExited();
        }

        public void ReleasePending()
        {
            m_MainScene.OnMenuExited();
        }
    }
}
