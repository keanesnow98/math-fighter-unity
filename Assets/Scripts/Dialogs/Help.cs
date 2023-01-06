using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MathFighter.Dialogs
{
    public class Help : MonoBehaviour
    {
        private int currentIndex;

        [SerializeField]
        public RawImage m_CurrentImage;

        // Start is called before the first frame update
        void Start()
        {
            currentIndex = 0;
        }

        // Update is called once per frame
        void Update()
        {
        }

        public void OnLeftTabButtonClicked()
        {
            if (currentIndex > 0) currentIndex--;
            m_CurrentImage.texture = Resources.Load<Texture2D>("UI/Sprites/Help/How to play " + (currentIndex + 1));
        }

        public void OnRightTabButtonClicked()
        {
            if (currentIndex < 2) currentIndex++;
            m_CurrentImage.texture = Resources.Load<Texture2D>("UI/Sprites/Help/How to play " + (currentIndex + 1));
        }
    }
}
