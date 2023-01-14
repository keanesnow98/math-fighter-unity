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
using MathFreak.GamePlay;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Net;
using System.Diagnostics;
using MathFreak.AsyncTaskFramework;



namespace MathFreak.GameStates
{
    /// <summary>
    /// This gamestate handles local match screen for setting up a local multiplayer match
    /// </summary>
    [TorqueXmlSchemaType]
    public class GameStateLocalMatch : GameState
    {
        private static GameStateLocalMatch _this;

        private TransitionHorizontalFlyon _flyonTransition;
        private TransitionFallOff _falloffTransition;
        private const float BUTTON_FLYON_DELAY = 0.1f;
        private const float BUTTON_FALLOFF_DELAY = 0.1f;

        private const int MIN_TARGETSCORE = 250;
        private const int MAX_TARGETSCORE = 10000;
        private const int STEP_TARGETSCORE = 250;
        private int _targetScore;

        public override void Init()
        {
            base.Init();

            Assert.Fatal(_this == null, "Should never be creating more than one instance of GameStateLocalMatch");

            _this = this;
            _targetScore = 500;  // default target - player can alter this as they choose
        }

        public override void PreTransitionOn(string paramString)
        {
            base.PreTransitionOn(paramString);

            Game.Instance.LoadScene(@"data\levels\LocalMatch.txscene");

            // setup the scene
            UpdateOptionDisplays();

            // set up the flyon transition
            _flyonTransition = new TransitionHorizontalFlyon();
            _flyonTransition.ObjFlyonDelay = BUTTON_FLYON_DELAY;

            _flyonTransition.Add(TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("buttonlocalmatch_play"));
            _flyonTransition.Add(TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("buttonlocalmatch_targetscore"));
            _flyonTransition.Add(TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("buttonlocalmatch_back"));
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

            _falloffTransition.Add(TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("buttonlocalmatch_back"));
            _falloffTransition.Add(TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("buttonlocalmatch_targetscore"));
            _falloffTransition.Add(TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("buttonlocalmatch_play"));
        }                                                                                  

        public override bool TickTransitionOff(float dt)
        {
            base.TickTransitionOff(dt);

            return _falloffTransition.tick(dt);
        }

        public override void OnTransitionOffCompleted()
        {
            base.OnTransitionOffCompleted();

            Game.Instance.UnloadScene(@"data\levels\LocalMatch.txscene");
        }

        private void UpdateOptionDisplays()
        {
            TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("buttonlocalmatch_targetscore").Components.FindComponent<MFTextOptionButton>().TextLabel = "Target: " + _targetScore;
        }

#if XBOX
        /// <summary>
        /// This task will move the game to the lobby gamestate if there are enough players signed in.
        /// If not enough players are signed in it will show the sign-in dialog to allow more players
        /// to sign in.  After showing the dialog, if enough players have signed in then it will move
        /// the game to the lobby state else it will leave the game where it is (in the localmatch state).
        /// </summary>
        private static IEnumerator<AsyncTaskStatus> AsyncTask_GotoLobby()
        {
            // if there aren't enough players are signed in then ask them to sign in
            if (SignedInGamer.SignedInGamers.Count < 2)
            {
                // if the guide is already showing then we can't show the sign-in dialog so just quit this task
                if (Guide.IsVisible)
                {
                    Debug.WriteLine("multiplayer local match - ShowSignIn() - guide already showing");
                    yield break;
                }

                // allow players to sign in
                Guide.ShowSignIn(2, false);

                // pause for one tick so the guide has a chance to move to the IsVisible state
                //yield return null;

                // wait for the sign-in dialog to dissappear
                while (Guide.IsVisible) yield return null;

                Debug.WriteLine("Guide has closed now...");

                // if enough players still aren't signed in then quit this task
                if (SignedInGamer.SignedInGamers.Count < 2)
                {
                    Debug.WriteLine("multiplayer local match - ShowSignIn() - not enough players are signed in");
                    yield break;;
                }
            }

            // setup the game and move to the lobby state

            //// ...create a game settings object for the gameplay to use
            //GamePlaySettingsMultiplayer settings = new GamePlaySettingsMultiplayer();
            //Game.Instance.ActiveGameplaySettings = settings;

            //// ...set up settings that the user has selected
            //settings.Dealer = new QuestionDealer(1);
            //settings.ScoreTarget = _this._targetScore;

            // create a local session (local multiplayer is basically the same as LIVE multiplayer in this regard)

            // this is a local session so no need to create the session asynchronously, but we do need to make sure
            // we catch any exceptions
            try
            {
                NetworkSession session = NetworkSession.Create(NetworkSessionType.Local, 2, 2);
                NetworkSessionManager.Instance.Session = session;
            }
            catch (Exception e)
            {
                Assert.Fatal(false, "Local multiplayer match - can't create local session: " + e.Message);
                yield break;
            }

            // let's all go to the lobby...
            GameStateManager.Instance.Push(GameStateNames.LOBBY, GameStateNames.GAMEPLAY_MULTIPLAYER_LOCAL);
        }
#endif


        public static GUIActionDelegate Play { get { return _play; } }
        public static GUIActionDelegate TargetScoreDecrease { get { return _targetScoreDecrease; } }
        public static GUIActionDelegate TargetScoreIncrease { get { return _targetScoreIncrease; } }

        private static GUIActionDelegate _play = new GUIActionDelegate(delegate(GUIComponent guiComponent, string paramString)
        {
#if XBOX
            Debug.WriteLine("(Action) pushing gamestate Lobby");
            Game.Instance.AddAsyncTask(AsyncTask_GotoLobby(), false);
#else
            // PC version can't do multiplayer stuff
            Debug.WriteLine("(Action) (PC version) *not* pushing gamestate Lobby");
#endif
        });

        private static GUIActionDelegate _targetScoreDecrease = new GUIActionDelegate(delegate(GUIComponent guiComponent, string paramString)
        {
            Debug.WriteLine("(Action) decreasing target score via local match screen");

            _this._targetScore -= STEP_TARGETSCORE;

            if (_this._targetScore < MIN_TARGETSCORE)
            {
                _this._targetScore = MIN_TARGETSCORE;
            }

            _this.UpdateOptionDisplays();
        });

        private static GUIActionDelegate _targetScoreIncrease = new GUIActionDelegate(delegate(GUIComponent guiComponent, string paramString)
        {
            Debug.WriteLine("(Action) increasing target score via local match screen");

            _this._targetScore += STEP_TARGETSCORE;

            if (_this._targetScore > MAX_TARGETSCORE)
            {
                _this._targetScore = MAX_TARGETSCORE;
            }

            _this.UpdateOptionDisplays();
        });
    }
}
