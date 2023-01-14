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
using Microsoft.Xna.Framework.Net;
using MathFreak.GameStates.Dialogs;
using Microsoft.Xna.Framework.GamerServices;
using MathFreak.GamePlay;
using MathFreak.AsyncTaskFramework;



namespace MathFreak.GameStates
{
    /// <summary>
    /// This gamestate handles the menu screen for choosing whether to join or create an Xbox LIVE match
    /// </summary>
    [TorqueXmlSchemaType]
    public class GameStateXboxLIVE : GameState
    {
        private GameStateXboxLIVE _this;

        private TransitionHorizontalFlyon _flyonTransition;
        private TransitionFallOff _falloffTransition;
        private const float BUTTON_FLYON_DELAY = 0.1f;
        private const float BUTTON_FALLOFF_DELAY = 0.1f;

        private static string _lastSelectedButton;

        public override void Init()
        {
            Assert.Fatal(_this == null, "Should not have more than one instancce of GameStateXboxLIVE");
            base.Init();
            _this = this;
        }

        public override void PreTransitionOn(string paramString)
        {
            base.PreTransitionOn(paramString);

            if (paramString == RESET_LASTSELECTED)
            {
                _lastSelectedButton = null;
            }

            FEUIbackground.Instance.Load();

            Game.Instance.LoadScene(@"data\levels\XboxLIVE.txscene");

            TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("xboxlive_progressanim").Visible = false;

            // set up the flyon transition
            _flyonTransition = new TransitionHorizontalFlyon();
            _flyonTransition.ObjFlyonDelay = BUTTON_FLYON_DELAY;

            _flyonTransition.Add(TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("buttonxboxlive_quickmatch"));
            _flyonTransition.Add(TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("buttonxboxlive_choosematch"));
            _flyonTransition.Add(TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("buttonxboxlive_creatematch"));
            _flyonTransition.Add(TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("buttonxboxlive_back"));
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

            _falloffTransition.Add(TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("buttonxboxlive_back"));
            _falloffTransition.Add(TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("buttonxboxlive_creatematch"));
            _falloffTransition.Add(TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("buttonxboxlive_choosematch"));
            _falloffTransition.Add(TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("buttonxboxlive_quickmatch"));
        }                                                                                  

        public override bool TickTransitionOff(float dt)
        {
            base.TickTransitionOff(dt);

            return _falloffTransition.tick(dt);
        }

        public override void OnTransitionOffCompleted()
        {
            base.OnTransitionOffCompleted();

            Game.Instance.UnloadScene(@"data\levels\XboxLIVE.txscene");
        }

//#if XBOX
        /// <summary>
        /// Will create a LIVE session and then move to the settings-lobby gamestate.
        /// NOTE: unlike the local session creation this one has to be done async.
        /// </summary>
        private static IEnumerator<AsyncTaskStatus> AsyncTask_CreateMatchAndGotoSettingsLobbyLIVE()
        {
            // disable the GUI while we are creating a session
            GUIManager.Instance.DeactivateGUI();

            // hide the menu buttons - we will show a 'Creating...' message instead
            TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("buttonxboxlive_back").Visible = false;
            TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("buttonxboxlive_creatematch").Visible = false;
            TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("buttonxboxlive_choosematch").Visible = false;
            TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("buttonxboxlive_quickmatch").Visible = false;

            // show progress anim
            TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("xboxlive_progressanim").Visible = true;

            // create a LIVE session
            IAsyncResult result;

            // create the game settings object
            Game.Instance.ActiveGameplaySettings = new GamePlaySettingsMultiplayerLIVE(Game.Instance.LastLIVESettings);

            try
            {
                result = NetworkSessionManager.Instance.BeginCreate(NetworkSessionManager.PLAYERMATCH, 1, 2, 0, GamePlaySettings.GetInitialSessionCreationProperties(), null, null);
            }
            catch (Exception e) // generic exception catching as we just want to abort if anything goes wrong
            {
                // something went really wrong - abort!
                Assert.Warn(false, "Warning: Could not create LIVE multiplayer match - BeginCreate() threw an exception:\n" + e.Message);
                NetworkSessionManager.Instance.ShutdownSession();   // incase the error was due to already having a session active
                TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("xboxlive_progressanim").Visible = false;
                DialogNotification.Show(DialogNotification.MESSAGE_COULDNOTCREATEMATCH, GUIActions.PopGameState);
                yield break;
            }

            // Wait until the session creation completes
            while (!result.IsCompleted) yield return null;

            // get the session
            NetworkSession session = null;

            try
            {
                session = NetworkSessionManager.Instance.EndCreate(result);
            }
            catch (Exception e)
            {
                Assert.Warn(false, "Warning: EndCreate() threw an exception (usually because a network session already exists): " + e.Message);
                NetworkSessionManager.Instance.ShutdownSession();   // incase the error was due to already having a session active
                TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("xboxlive_progressanim").Visible = false;
            }

            // if we failed to create a session then let the player know
            if (session == null)
            {
                Assert.Warn(false, "Warning: Could not create LIVE multiplayer match - BeginCreate() returned null");
                NetworkSessionManager.Instance.ShutdownSession();   // incase the error was due to already having a session active
                TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("xboxlive_progressanim").Visible = false;
                DialogNotification.Show(DialogNotification.MESSAGE_COULDNOTCREATEMATCH, GUIActions.PopGameState);
                yield break;
            }

            // else set the newly created session as the active one
            NetworkSessionManager.Instance.Session = session;

            // add the host to the list of players
            foreach (SignedInGamer gamer in SignedInGamer.SignedInGamers)
            {
                if (gamer.Gamertag == session.Host.Gamertag)
                {
                    Game.Instance.ActiveGameplaySettings.AddPlayer(new PlayerLocal((int)gamer.PlayerIndex));
                    break;
                }
            }

            // hide progress anim
            TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("xboxlive_progressanim").Visible = false;

            // let's all go to the lobby...
            GameStateManager.Instance.Push(GameStateNames.SETTINGS_MULTIPLAYER_LIVE, null);
        }
//#endif


        public static GUIActionDelegate QuickMatch { get { return _quickmatch; } }
        public static GUIActionDelegate ChooseMatch { get { return _choosematch; } }
        public static GUIActionDelegate CreateMatch { get { return _creatematch; } }

        private static GUIActionDelegate _quickmatch = new GUIActionDelegate(delegate(GUIComponent guiComponent, string paramString)
        {
            Debug.WriteLine("(Action) pushing gamestate QuickMatch");
            _lastSelectedButton = GUIManager.Instance.GetFocused().Owner.Name;
            GameStateManager.Instance.Push(GameStateNames.QUICKMATCH, GameState.RESET_LASTSELECTED);
        });

        private static GUIActionDelegate _choosematch = new GUIActionDelegate(delegate(GUIComponent guiComponent, string paramString)
        {
            Debug.WriteLine("(Action) pushing gamestate ChooseMatch");
            _lastSelectedButton = GUIManager.Instance.GetFocused().Owner.Name;
            GameStateManager.Instance.Push(GameStateNames.CHOOSEMATCH, GameState.RESET_LASTSELECTED);
        });

        private static GUIActionDelegate _creatematch = new GUIActionDelegate(delegate(GUIComponent guiComponent, string paramString)
        {
            Debug.WriteLine("(Action) pushing gamestate LIVE Multiplayer Settings");

//#if XBOX
            Game.Instance.AddAsyncTask(AsyncTask_CreateMatchAndGotoSettingsLobbyLIVE(), true);
//#else
//            // PC version can't do multiplayer stuff
//            Debug.WriteLine("(Action) (PC) *not* pushing gamestate LIVE Multiplayer Settings");
//#endif
        });
    }
}
