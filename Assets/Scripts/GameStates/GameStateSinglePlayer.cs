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
using MathFreak.GameStates.Transitions;
using System.Diagnostics;
using MathFreak.Math;
using MathFreak.Math.Questions;
using MathFreak.GamePlay;



namespace MathFreak.GameStates
{
    /// <summary>
    /// This gamestate handles the menu screen for selecting a single-player game mode to play
    /// </summary>
    [TorqueXmlSchemaType]
    public class GameStateSinglePlayer : GameState
    {
        private TransitionHorizontalFlyon _flyonTransition;
        private TransitionFallOff _falloffTransition;
        private const float BUTTON_FLYON_DELAY = 0.1f;
        private const float BUTTON_FALLOFF_DELAY = 0.1f;

        private static string _lastSelectedButton;

        public override void Init()
        {
            base.Init();

            // nothing to do yet
        }

        public override void PreTransitionOn(string paramString)
        {
            base.PreTransitionOn(paramString);

            if (paramString == RESET_LASTSELECTED)
            {
                _lastSelectedButton = null;
            }

            Game.Instance.LoadScene(@"data\levels\SinglePlayer.txscene");

            // set up the flyon transition
            _flyonTransition = new TransitionHorizontalFlyon();
            _flyonTransition.ObjFlyonDelay = BUTTON_FLYON_DELAY;

            _flyonTransition.Add(TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("buttonsingleplayer_teachme"));
            _flyonTransition.Add(TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("buttonsingleplayer_freak"));
            _flyonTransition.Add(TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("buttonsingleplayer_back"));

            // TESTING
            // dynamic questions and answers

            //MathQuestion mq;

            ////mq = new DMQ_SimpleAddition();
            ////mq = new DMQ_SimpleSubtraction();
            ////mq = new DMQ_SimpleMultiplication();
            ////mq = new DMQ_SimpleDivision();
            ////mq = new DMQ_FractionToDecimal();
            //mq = new DMQ_DecimalToPercentage();

            //string[] content = mq.GetContent();

            //Debug.WriteLine("Question: " + content[0]);
            //Debug.WriteLine("Answer 1: " + content[1]);
            //Debug.WriteLine("Answer 2: " + content[2]);
            //Debug.WriteLine("Answer 3: " + content[3]);
            //Debug.WriteLine("Answer 4: " + content[4]);

            //MathExpression expression = MathExpression.Parse(content[0]);
            //T2DStaticSprite expressionSprite = expression.GenerateSprite(50, 5, Game.GetEnumeratedFont(Game.EnumMathFreakFont.Default), new Vector4(0.5f, 0.5f, 0.8f, 0.9f));
            //expressionSprite.Position = new Vector2(-640.0f + 50.0f + (expressionSprite.Size.X * 0.5f), -370.0f + 50.0f + (expressionSprite.Size.Y * 0.5f));
            //TorqueObjectDatabase.Instance.Register(expressionSprite);

            //expression = MathExpression.Parse(content[1]);
            //expressionSprite = expression.GenerateSprite(50, 5, Game.GetEnumeratedFont(Game.EnumMathFreakFont.Default), new Vector4(0.5f, 0.8f, 0.5f, 0.9f));
            //expressionSprite.Position = new Vector2(-640.0f + 50.0f + (expressionSprite.Size.X * 0.5f), -370.0f + 200.0f + (expressionSprite.Size.Y * 0.5f));
            //TorqueObjectDatabase.Instance.Register(expressionSprite);

            //expression = MathExpression.Parse(content[2]);
            //expressionSprite = expression.GenerateSprite(50, 5, Game.GetEnumeratedFont(Game.EnumMathFreakFont.Default), new Vector4(0.8f, 0.5f, 0.5f, 0.9f));
            //expressionSprite.Position = new Vector2(-640.0f + 50.0f + (expressionSprite.Size.X * 0.5f), -370.0f + 275.0f + (expressionSprite.Size.Y * 0.5f));
            //TorqueObjectDatabase.Instance.Register(expressionSprite);

            //expression = MathExpression.Parse(content[3]);
            //expressionSprite = expression.GenerateSprite(50, 5, Game.GetEnumeratedFont(Game.EnumMathFreakFont.Default), new Vector4(0.8f, 0.5f, 0.5f, 0.9f));
            //expressionSprite.Position = new Vector2(-640.0f + 50.0f + (expressionSprite.Size.X * 0.5f), -370.0f + 350.0f + (expressionSprite.Size.Y * 0.5f));
            //TorqueObjectDatabase.Instance.Register(expressionSprite);

            //expression = MathExpression.Parse(content[4]);
            //expressionSprite = expression.GenerateSprite(50, 5, Game.GetEnumeratedFont(Game.EnumMathFreakFont.Default), new Vector4(0.8f, 0.5f, 0.5f, 0.9f));
            //expressionSprite.Position = new Vector2(-640.0f + 50.0f + (expressionSprite.Size.X * 0.5f), -370.0f + 425.0f + (expressionSprite.Size.Y * 0.5f));
            //TorqueObjectDatabase.Instance.Register(expressionSprite);

            // TESTING ends
        }

        public override bool TickTransitionOn(float dt, bool prevStateHasTransitionedOff)
        {
            base.TickTransitionOn(dt, prevStateHasTransitionedOff);

            if (!prevStateHasTransitionedOff) return false;

            // process the transition
            return _flyonTransition.tick(dt);
        }

        public override void OnTransitionOnCompleted()
        {
            base.OnTransitionOnCompleted();

            if (_lastSelectedButton != null)
            {
                GUIManager.Instance.SetFocus(TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>(_lastSelectedButton).Components.FindComponent<GUIComponent>());
                _lastSelectedButton = null;
            }

            GUIManager.Instance.ActivateGUI();
        }

        public override void PreTransitionOff()
        {
            base.PreTransitionOff();

            // make sure if stuff is still flying on that we cancel that transition
            _flyonTransition.CancelAll();

            // set up the fall off transition
            _falloffTransition = new TransitionFallOff();
            _falloffTransition.ObjFalloffDelay = BUTTON_FALLOFF_DELAY;

            _falloffTransition.Add(TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("buttonsingleplayer_back"));
            _falloffTransition.Add(TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("buttonsingleplayer_freak"));
            _falloffTransition.Add(TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("buttonsingleplayer_teachme"));
        }                                                                                  

        public override bool TickTransitionOff(float dt)
        {
            base.TickTransitionOff(dt);

            return _falloffTransition.tick(dt);
        }

        public override void OnTransitionOffCompleted()
        {
            base.OnTransitionOffCompleted();

            Game.Instance.UnloadScene(@"data\levels\SinglePlayer.txscene");
        }

        public static GUIActionDelegate Freak { get { return _freak; } }
        public static GUIActionDelegate PlayTeachMe { get { return _playTeachMe; } }

        private static GUIActionDelegate _freak = new GUIActionDelegate(delegate(GUIComponent guiComponent, string paramString)
        {
            Debug.WriteLine("(Action) pushing gamestate Freak");
            _lastSelectedButton = GUIManager.Instance.GetFocused().Owner.Name;
            GameStateManager.Instance.Push(GameStateNames.FREAK, GameState.RESET_LASTSELECTED);
        });

        private static GUIActionDelegate _playTeachMe = new GUIActionDelegate(delegate(GUIComponent guiComponent, string paramString)
        {
            Debug.WriteLine("(Action) pushing gamestate GamePlayTeachMe");

            // create a game settings object for the gameplay to use
            GamePlaySettings settings = new GamePlaySettings();
            Game.Instance.ActiveGameplaySettings = settings;

            // Add the active player to the game settings
            settings.AddPlayer(Game.Instance.GetLocalPlayer());
            
            // Setup a question dealer and assign it to the game settings
            settings.Dealer = new QuestionDealer(1);

            // Play ball!!!
            GameStateManager.Instance.Push(GameStateNames.GAMEPLAY_TEACHME, null);
        });
    }
}
