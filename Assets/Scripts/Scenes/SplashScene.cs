using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MathFighter.Scenes
{
    public class SplashScene : MonoBehaviour
    {
        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            #if UNITY_EDITOR
                if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
                    ShiftScene();
            
            #else

                if (Input.touchCount > 0)
                    ShiftScene();

            #endif
        }

        public void ShiftScene()
        {
            SceneManager.LoadScene("MainScene");
        }
    }
}