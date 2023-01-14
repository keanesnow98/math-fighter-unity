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
using Microsoft.Xna.Framework.Media;
using MathFreak.AsyncTaskFramework;
using MathFreak.ContentLoading;



namespace MathFreak.GameStates
{
    /// <summary>
    /// This gamestate plays the iBlade splashscreen movie
    /// </summary>
    class GameStateSplash_iBlade : GameState
    {
        private T2DStaticSprite _videoHostObj;
        private VideoComponent _video;


        public override void Init()
        {
            base.Init();

            // nothing to do yet
        }

        public override void PreTransitionOn(string paramString)
        {
            base.PreTransitionOn(paramString);

            Game.Instance.LoadScene(@"data\levels\iBladeSplash.txscene");
        }

        public override void OnTransitionOnCompleted()
        {
            base.OnTransitionOnCompleted();

            _videoHostObj = TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("ibladesplash_video");
            _video = _videoHostObj.Components.FindComponent<VideoComponent>();
            _videoHostObj.Material.IsTranslucent = true;
            (_videoHostObj.Material as SimpleMaterial).IsColorBlended = true;

            Game.Instance.AddAsyncTask(Async_PlayVideoAndFadeOut(), true);
        }

        public override void PreTransitionOff()
        {
            base.PreTransitionOff();

            // unload the video asset (and any other assets in this scene)
            _videoHostObj.Visible = false;
            _videoHostObj.MarkForDelete = true;
        }

        public override void OnTransitionOffCompleted()
        {
            base.OnTransitionOffCompleted();
            Game.Instance.UnloadScene(@"data\levels\iBladeSplash.txscene");
        }

        private IEnumerator<AsyncTaskStatus> Async_PlayVideoAndFadeOut()
        {
            // play video
            _video.Play();

            // wait for video to finish
            while (_video.VideoPlayerState == MediaState.Playing) yield return null;

            // start content loading of art assets so that this will happen while the remaining splash screens are showing
            Game.Instance.SharedContent.AsyncLoadImmediately(null, null);

            // wait a while so player can see the logo splash
            float elapsed = 0.0f;

            while (elapsed < 2.0f)
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

                _videoHostObj.VisibilityLevel = 1.0f - (elapsed / FADEOUT_DURATION);

                yield return null;
            }

            // load the next splashscreen
            GameStateManager.Instance.ClearAndLoad(GameStateNames.SPLASH_TX, null);
        }
    }
}
