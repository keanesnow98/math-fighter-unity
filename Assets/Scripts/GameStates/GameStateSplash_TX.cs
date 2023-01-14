using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

using GarageGames.Torque.GameUtil;
using GarageGames.Torque.Core;
using GarageGames.Torque.Materials;
//using GarageGames.Torque.SceneGraph;
using GarageGames.Torque.Sim;
//using GarageGames.Torque.GameUtil;
using GarageGames.Torque.T2D;
using GarageGames.Torque.Platform;

using MathFreak.GameStateFramework;
using MathFreak.GUIFrameWork;
using System.Threading;
using MathFreak.Text;
using MathFreak.AsyncTaskFramework;
using MathFreak.ContentLoading;



namespace MathFreak.GameStates
{
    /// <summary>
    /// This gamestate does the TX splashscreen
    /// </summary>
    class GameStateSplash_TX : GameState
    {
        private T2DStaticSprite _image;


        public override void Init()
        {
            base.Init();

            // nothing to do yet
        }

        public override void PreTransitionOn(string paramString)
        {
            base.PreTransitionOn(paramString);

            Game.Instance.LoadScene(@"data\levels\TXSplash.txscene");
        }

        public override void OnTransitionOnCompleted()
        {
            base.OnTransitionOnCompleted();

            _image = TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("txsplash_image");
            _image.Material.IsTranslucent = true;
            (_image.Material as SimpleMaterial).IsColorBlended = true;

            _image.VisibilityLevel = 0.0f;

            Game.Instance.AddAsyncTask(Async_FadeInAndOut(), true);
        }

        public override bool TickTransitionOff(float dt)
        {
            return base.TickTransitionOff(dt);
        }

        public override void OnTransitionOffCompleted()
        {
            base.OnTransitionOffCompleted();

            Game.Instance.UnloadScene(@"data\levels\TXSplash.txscene");
        }

        private IEnumerator<AsyncTaskStatus> Async_FadeInAndOut()
        {
            // fade in the logo splash
            float elapsed = 0.0f;
            const float FADEIN_DURATION = 1.0f;

            while (elapsed < FADEIN_DURATION)
            {
                elapsed += Game.Instance.dt;

                if (elapsed > FADEIN_DURATION)
                {
                    elapsed = FADEIN_DURATION;
                }

                _image.VisibilityLevel = elapsed / FADEIN_DURATION;

                yield return null;
            }

            // wait a few seconds so player can see the logo splash
            elapsed = 0.0f;

            while (elapsed < 4.0f)
            {
                elapsed += Game.Instance.dt;
                yield return null;
            }

            // fade out the logo splash
            elapsed = 0.0f;
            const float FADEOUT_DURATION = 0.5f;

            while (elapsed < FADEOUT_DURATION)
            {
                elapsed += Game.Instance.dt;

                if (elapsed > FADEOUT_DURATION)
                {
                    elapsed = FADEOUT_DURATION;
                }

                _image.VisibilityLevel = 1.0f - (elapsed / FADEOUT_DURATION);

                yield return null;
            }

            //// if finished loading content then go straight to the intro vid
            //if (Game.Instance.SharedContent.AsyncResult.IsCompleted)
            //{
                GameStateManager.Instance.ClearAndLoad(GameStateNames.SPLASH_INTRO_VID, null);
            //}
            //// else show the loading screen while content loading completes (if player is loading from a memory stick it could take a while longer than from HD)
            //else
            //{
            //    GameStateManager.Instance.ClearAndLoad(GameStateNames.LOADCONTENT, null);
            //}
        }
    }
}
