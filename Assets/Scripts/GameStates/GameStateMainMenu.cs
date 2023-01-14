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
using MathFreak.Highscores;



namespace MathFreak.GameStates
{
    /// <summary>
    /// This gamestate handles the mainmenu
    /// </summary>
    [TorqueXmlSchemaType]
    public class GameStateMainMenu : GameState
    {
        public const string RETURNING_FROM_GAME = "returningfromgame";  // used as param when moving to the main menu state, to tell the main menu that we are returning directly from the game screen (will need to do things differently)

        private float _delayTransitionOn;
        private TransitionHorizontalFlyon _flyonTransition;
        private TransitionFallOff _falloffTransition;
        private const float BUTTON_FLYON_DELAY = 0.1f;
        private const float BUTTON_FALLOFF_DELAY = 0.05f;
        private bool _isFollowingTitle;

        private static string _lastSelectedButton;

        public override void Init()
        {
            base.Init();

            // nothing to do yet
        }

        public override void PreTransitionOn(string paramString)
        {
            base.PreTransitionOn(paramString);

            if (paramString == RESET_LASTSELECTED || paramString == RETURNING_FROM_GAME)
            {
                _lastSelectedButton = null;
            }

            // if not returning from the game screen then set up for the transition right away
            if (paramString != RETURNING_FROM_GAME)
            {
                SetupTransitionOn();
            }
        }

        private void SetupTransitionOn()
        {
            FEUIbackground.Instance.Load();

            Game.Instance.LoadScene(@"data\levels\MainMenu.txscene");

            FEUIbackground.PlayMusic();

            // set up the flyon transition
            _flyonTransition = new TransitionHorizontalFlyon();
            _flyonTransition.ObjFlyonDelay = BUTTON_FLYON_DELAY;

            _flyonTransition.Add(TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("buttonsingleplayer"));
            _flyonTransition.Add(TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("buttonmultiplayer"));
            _flyonTransition.Add(TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("buttonhighscores"));
            _flyonTransition.Add(TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("buttonoptions"));
            _flyonTransition.Add(TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("buttonhowtoplay"));
            _flyonTransition.Add(TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("buttontellafriend"));
            _flyonTransition.Add(TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("buttonquit"));

            // if the title screen is the one transitioning off then we need to delay by the amount that the title screen is delaying it's own transition
            _isFollowingTitle = (GameStateManager.Instance.GetNameOf(GameStateManager.Instance.TransitioningOffState) == GameStateNames.TITLE);

            if (_isFollowingTitle)
            {
                _delayTransitionOn = GameStateTitle.TRANSITION_DELAY;
            }
            else
            {
                _delayTransitionOn = 0.0f;
            }
        }

        public override bool TickTransitionOn(float dt, bool prevStateHasTransitionedOff)
        {
            base.TickTransitionOn(dt, prevStateHasTransitionedOff);

            // if we are returning from the game screen then we won't be transitioning on - just appearing, once the game screen has transitioned off
            if (ParamString == RETURNING_FROM_GAME) return true;

            // delay the transition
            _delayTransitionOn -= dt;

            if (_delayTransitionOn > 0.0f) return false;

            if (!_isFollowingTitle && !prevStateHasTransitionedOff) return false;

            // process the transition
            return _flyonTransition.tick(dt);
        }

        public override void OnTransitionOnCompleted()
        {
            base.OnTransitionOnCompleted();

            // if returning from the game then load the scene and FEUI, and set them up
            if (ParamString == RETURNING_FROM_GAME)
            {
                FEUIbackground.Instance.Load();

                Game.Instance.LoadScene(@"data\levels\MainMenu.txscene");

                FEUIbackground.PlayMusic();

                // position and size the MF logo
                T2DSceneObject mfLogo = TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("mathfreaklogo");
                mfLogo.Position = new Vector2(GameStateTitle.MFLOGO_TARGET_X, GameStateTitle.MFLOGO_TARGET_Y);
                mfLogo.Size = new Vector2(GameStateTitle.MFLOGO_TARGET_WIDTH, GameStateTitle.MFLOGO_TARGET_HEIGHT);
            }

            if (_lastSelectedButton != null)
            {
                T2DSceneObject guiobject = TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>(_lastSelectedButton);

                if (guiobject != null)  // for some reason we *can* have an bad object referenced here, so silliness check before we try and set the focus
                {
                    GUIManager.Instance.SetFocus(guiobject.Components.FindComponent<GUIComponent>());
                }

                _lastSelectedButton = null;
            }

            //HighscoreData.Instance.Save();

            GUIManager.Instance.ActivateGUI();

            // enable invites - if player is joining from outside the game then we are now in a
            // position to process that.
            NetworkSessionManager.Instance.EnableInvites();
        }

        public override void PreTransitionOff()
        {
            base.PreTransitionOff();

            // make sure if stuff is still flying on that we cancel that transition
            _flyonTransition.CancelAll();
                
            // set up the fall off transition
            _falloffTransition = new TransitionFallOff();
            _falloffTransition.ObjFalloffDelay = BUTTON_FALLOFF_DELAY;

            _falloffTransition.Add(TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("buttonquit"));
            _falloffTransition.Add(TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("buttontellafriend"));
            _falloffTransition.Add(TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("buttonhowtoplay"));
            _falloffTransition.Add(TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("buttonoptions"));
            _falloffTransition.Add(TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("buttonhighscores"));
            _falloffTransition.Add(TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("buttonmultiplayer"));
            _falloffTransition.Add(TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("buttonsingleplayer"));
        }

        public override bool TickTransitionOff(float dt)
        {
            base.TickTransitionOff(dt);

            return _falloffTransition.tick(dt);
        }

        public override void OnTransitionOffCompleted()
        {
            base.OnTransitionOffCompleted();

            DoUnload();
        }

        public override void UnloadedImmediately()
        {
            base.UnloadedImmediately();

            DoUnload();
        }

        private void DoUnload()
        {
            Game.Instance.UnloadScene(@"data\levels\MainMenu.txscene");
        }

        public static GUIActionDelegate Options { get { return _options; } }
        public static GUIActionDelegate SinglePlayer { get { return _singleplayer; } }
        public static GUIActionDelegate MultiPlayer { get { return _multiplayer; } }
        public static GUIActionDelegate HighScores { get { return _highscores; } }
        public static GUIActionDelegate HowToPlay { get { return _howtoplay; } }

        private static GUIActionDelegate _options = new GUIActionDelegate(delegate(GUIComponent guiComponent, string paramString)
        {
            Debug.WriteLine("(Action) pushing gamestate Options");
            _lastSelectedButton = GUIManager.Instance.GetFocused().Owner.Name;
            GameStateManager.Instance.Push(GameStateNames.OPTIONS, GameState.RESET_LASTSELECTED);
        });

        private static GUIActionDelegate _singleplayer = new GUIActionDelegate(delegate(GUIComponent guiComponent, string paramString)
        {
            Debug.WriteLine("(Action) pushing gamestate SinglePlayer Settings");
            _lastSelectedButton = GUIManager.Instance.GetFocused().Owner.Name;
            GameStateManager.Instance.Push(GameStateNames.SETTINGS_SINGLEPLAYER, GameState.RESET_LASTSELECTED);
        });

        private static GUIActionDelegate _multiplayer = new GUIActionDelegate(delegate(GUIComponent guiComponent, string paramString)
        {
            Debug.WriteLine("(Action) pushing gamestate MultiPlayer");
            _lastSelectedButton = GUIManager.Instance.GetFocused().Owner.Name;
            GameStateManager.Instance.Push(GameStateNames.MULTIPLAYER, GameState.RESET_LASTSELECTED);
        });

        private static GUIActionDelegate _highscores = new GUIActionDelegate(delegate(GUIComponent guiComponent, string paramString)
        {
            Debug.WriteLine("(Action) pushing gamestate HighScores");
            _lastSelectedButton = GUIManager.Instance.GetFocused().Owner.Name;
            GameStateManager.Instance.Push(GameStateNames.HIGHSCORES, GameState.RESET_LASTSELECTED);
        });

        private static GUIActionDelegate _howtoplay = new GUIActionDelegate(delegate(GUIComponent guiComponent, string paramString)
        {
            Debug.WriteLine("(Action) pushing gamestate HowToPlay");
            _lastSelectedButton = GUIManager.Instance.GetFocused().Owner.Name;
            GameStateManager.Instance.Push(GameStateNames.HOW_TO_PLAY, GameState.RESET_LASTSELECTED);
        });
    }
}
