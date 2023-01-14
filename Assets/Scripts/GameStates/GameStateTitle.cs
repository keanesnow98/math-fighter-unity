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
using GarageGames.Torque.Util;
using System.Diagnostics;
using MathFreak.Math;
using MathFreak.Math.Views;
using Microsoft.Xna.Framework.Graphics;



namespace MathFreak.GameStates
{
    /// <summary>
    /// This gamestate shows the title screen.  It shows a prompt to 'press start' and when
    /// start is pressed it detects which gamepad is active.
    /// </summary>
    class GameStateTitle : GameState
    {
//#if !XBOX
//        private float _gamepadDetectionTimeout;
//#endif

        public const float MFLOGO_TARGET_WIDTH = 306.0f;
        public const float MFLOGO_TARGET_HEIGHT = 190.0f;
        public const float MFLOGO_TARGET_X = 0.0f;
        public const float MFLOGO_TARGET_Y = -205.0f;

        private T2DSceneObject _mfLogo;
        private Vector2 _mfLogoTargetSize = new Vector2(MFLOGO_TARGET_WIDTH, MFLOGO_TARGET_HEIGHT);
        private Vector2 _mfLogoTargetPos = new Vector2(MFLOGO_TARGET_X, MFLOGO_TARGET_Y);
        private float _mfLogoStartPosY;
        private Vector2 _mfLogoStartSize;
        private const float TRANSISTION_DURATION = 0.4f;
        private float _transitionElapsed;
        private float _transitionDelay;

        public const float TRANSITION_DELAY = 1.0f;

        public override void Init()
        {
            base.Init();

            // nothing to do yet
        }

        public override void PreTransitionOn(string paramString)
        {
            base.PreTransitionOn(paramString);

            FEUIbackground.Instance.Load();

            Game.Instance.LoadScene(@"data\levels\Title.txscene");

//#if !XBOX
//            _gamepadDetectionTimeout = 1.0f;
//#endif
        }

        public override void OnTransitionOnCompleted()
        {
            base.OnTransitionOnCompleted();
        }

        public override void PreTransitionOff()
        {
            base.PreTransitionOff();

            Game.Instance.UnloadScene(@"data\levels\Title.txscene");

            _mfLogo = TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("mathfreaklogo");

            _mfLogoStartPosY = _mfLogo.Position.Y;
            _mfLogoStartSize = _mfLogo.Size;

            _transitionDelay = TRANSITION_DELAY;
            _transitionElapsed = 0.0f;
        }

        public override bool TickTransitionOff(float dt)
        {
            base.TickTransitionOff(dt);

            // delay the transition start - we do this to give the opportunity for the guide to disappear so the animation can be seen properly.
            _transitionDelay -= dt;

            if (_transitionDelay > 0.0f) return false;

            // do the animation stuff
            _transitionElapsed += dt;

            // shrink same speed on vertical and horizontal
            _mfLogo.Size = new Vector2(InterpolationHelper.Interpolate(_mfLogoStartSize.X, _mfLogoTargetSize.X, _transitionElapsed / TRANSISTION_DURATION, InterpolationMode.EaseIn), InterpolationHelper.Interpolate(_mfLogoStartSize.Y, _mfLogoTargetSize.Y, _transitionElapsed / TRANSISTION_DURATION, InterpolationMode.EaseIn));
            _mfLogo.Position = new Vector2(0.0f, InterpolationHelper.Interpolate(_mfLogoStartPosY, _mfLogoTargetPos.Y, _transitionElapsed / TRANSISTION_DURATION, InterpolationMode.EaseIn));

            // shrink faster on the vertical initially for a diff effect
            //_mfLogo.Size = new Vector2(InterpolationHelper.Interpolate(_mfLogoStartSize.X, _mfLogoTargetSize.X, _transitionElapsed / TRANSISTION_DURATION, InterpolationMode.EaseInOut), InterpolationHelper.Interpolate(_mfLogoStartSize.Y, _mfLogoTargetSize.Y, _transitionElapsed / TRANSISTION_DURATION, InterpolationMode.FlyOn));
            //_mfLogo.Position = new Vector2(0.5f, InterpolationHelper.Interpolate(_mfLogoStartPosY, _mfLogoTargetPos.Y, _transitionElapsed / TRANSISTION_DURATION, InterpolationMode.FlyOn));

            // check if animation finished
            if (_mfLogo.Size.Y <= _mfLogoTargetSize.Y)
            {
                //Debug.WriteLine("finished animating - elapsed time: " + _transitionElapsed);
                _mfLogo.Size = _mfLogoTargetSize;
                _mfLogo.Position = _mfLogoTargetPos;
                return true;
            }

            return false;
        }

        public override void OnTransitionOffCompleted()
        {
            base.OnTransitionOffCompleted();
        }

//#if !XBOX
//        // When testing on PC there might not be a gamepad connected so we will auto-detect it as "player 0" after a short time waiting
//        public override void Tick(float dt)
//        {
//            base.Tick(dt);

//            _gamepadDetectionTimeout -= dt;

//            if (_gamepadDetectionTimeout <= 0.0f)
//            {
//                Game.Instance.OnActiveGamepadDetected(0);
//            }
//        }
//#endif
    }
}
