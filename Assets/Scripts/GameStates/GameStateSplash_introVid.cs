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
    /// This gamestate plays the intro video
    /// </summary>
    class GameStateSplash_introVid : GameState
    {
        private T2DStaticSprite _videoHostObj;
        private VideoComponent _video;

        private bool _isExiting;


        public override void Init()
        {
            base.Init();

            // nothing to do yet
        }

        public override void PreTransitionOn(string paramString)
        {
            base.PreTransitionOn(paramString);

            _isExiting = false;
            Game.Instance.LoadScene(@"data\levels\IntroVidSplash.txscene");
        }

        public override void OnTransitionOnCompleted()
        {
            base.OnTransitionOnCompleted();

            _videoHostObj = TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("intro_vid");
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

            // unload the content used by the splashscreens
            Game.Instance.SplashContent.UnloadImmediately();
        }

        public override void OnTransitionOffCompleted()
        {
            base.OnTransitionOffCompleted();
            Game.Instance.UnloadScene(@"data\levels\IntroVidSplash.txscene");
        }

        public override void Tick(float dt)
        {
            base.Tick(dt);

            if (_isExiting) return;

            // check for pressing A or start on any gamepad
            for (PlayerIndex index = PlayerIndex.One; index <= PlayerIndex.Four; index++)
            {
                GamePadState gps = GamePad.GetState(index);

                if (gps.IsButtonDown(Buttons.Start) || gps.IsButtonDown(Buttons.A))
                {
                    _isExiting = true;
                    _video.Stop();
                    break;
                }
            }
        }

        private IEnumerator<AsyncTaskStatus> Async_PlayVideoAndFadeOut()
        {
            // play video
            _video.Play();

            // wait for video to finish
            while (_video.VideoPlayerState == MediaState.Playing) yield return null;

            _isExiting = true;

            // load the game title screen
            GameStateManager.Instance.ClearAndLoad(GameStateNames.TITLE, null);
        }
    }
}
