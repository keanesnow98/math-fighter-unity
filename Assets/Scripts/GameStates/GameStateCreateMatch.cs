//using System;
//using System.Collections.Generic;
//using System.Text;

//using Microsoft.Xna.Framework;
//using Microsoft.Xna.Framework.Input;

//using GarageGames.Torque.GameUtil;
//using GarageGames.Torque.Core;
//using GarageGames.Torque.Materials;
////using GarageGames.Torque.SceneGraph;
//using GarageGames.Torque.Sim;
////using GarageGames.Torque.GameUtil;
//using GarageGames.Torque.T2D;
//using GarageGames.Torque.Platform;

//using MathFreak.GameStateFramework;
//using MathFreak.GUIFrameWork;
//using System.Threading;
//using MathFreak.GameStates.Transitions;
//using MathFreak.GamePlay;
//using Microsoft.Xna.Framework.GamerServices;
//using Microsoft.Xna.Framework.Net;
//using System.Diagnostics;
//using MathFreak.AsyncTaskFramework;
//using MathFreak.GameStates.Dialogs;



//namespace MathFreak.GameStates
//{
//    /// <summary>
//    /// This gamestate handles create-match screen for hosting an xbox LIVE match
//    /// </summary>
//    [TorqueXmlSchemaType]
//    public class GameStateCreateMatch : GameState
//    {
//        private TransitionHorizontalFlyon _flyonTransition;
//        private TransitionFallOff _falloffTransition;
//        private const float BUTTON_FLYON_DELAY = 0.1f;
//        private const float BUTTON_FALLOFF_DELAY = 0.1f;

//        public override void Init()
//        {
//            base.Init();

//            // nothing to do here yet...
//        }

//        public override void PreTransitionOn(string paramString)
//        {
//            base.PreTransitionOn(paramString);

//            Game.Instance.LoadScene(@"data\levels\CreateMatch.txscene");

//            // hide progress anim
//            TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("creatematch_progressanim").Visible = false;

//            // set up the flyon transition
//            _flyonTransition = new TransitionHorizontalFlyon();
//            _flyonTransition.ObjFlyonDelay = BUTTON_FLYON_DELAY;

//            _flyonTransition.Add(TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("buttoncreatematch_play"));
//            _flyonTransition.Add(TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("buttoncreatematch_back"));
//        }

//        public override bool TickTransitionOn(float dt, bool prevStateHasTransitionedOff)
//        {
//            base.TickTransitionOn(dt, prevStateHasTransitionedOff);

//            if (!prevStateHasTransitionedOff) return false;

//            // process the transition
//            return _flyonTransition.tick(dt);
//        }

//        public override void OnTransitionOnCompleted()
//        {
//            base.OnTransitionOnCompleted();

//            GUIManager.Instance.ActivateGUI();
//        }

//        public override void PreTransitionOff()
//        {
//            base.PreTransitionOff();

//            // make sure if stuff is still flying on that we cancel that transition
//            _flyonTransition.CancelAll();

//            // set up the fall off transition
//            _falloffTransition = new TransitionFallOff();
//            _falloffTransition.ObjFalloffDelay = BUTTON_FALLOFF_DELAY;

//            _falloffTransition.Add(TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("buttoncreatematch_back"));
//            _falloffTransition.Add(TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("buttoncreatematch_play"));
//        }

//        public override bool TickTransitionOff(float dt)
//        {
//            base.TickTransitionOff(dt);

//            return _falloffTransition.tick(dt);
//        }

//        public override void OnTransitionOffCompleted()
//        {
//            base.OnTransitionOffCompleted();

//            Game.Instance.UnloadScene(@"data\levels\CreateMatch.txscene");
//        }

////#if XBOX
//        /// <summary>
//        /// Will create a LIVE session and then move to the settings-lobby gamestate.
//        /// NOTE: unlike the local session creation this one has to be done async.
//        /// </summary>
//        private static IEnumerator<AsyncTaskStatus> AsyncTask_GotoSettingsLobbyLIVE()
//        {
//            // disable the GUI while we are creating a session
//            GUIManager.Instance.DeactivateGUI();

//            // create a LIVE session
//            IAsyncResult result;

//            try
//            {
//                result = NetworkSessionManager.Instance.BeginCreate(NetworkSessionManager.PLAYERMATCH, 1, 3, 0, GamePlaySettings.GetInitialSessionCreationProperties(), null, null);
//            }
//            catch (Exception e) // generic exception catching as we just want to abort if anything goes wrong
//            {
//                // something went really wrong - abort!
//                Assert.Warn(false, "Warning: Could not create LIVE multiplayer match - BeginCreate() threw an exception:\n" + e.Message);
//                DialogNotification.Show(DialogNotification.MESSAGE_COULDNOTCREATEMATCH, GUIActions.PopGameState);
//                yield break;
//            }

//            // show progress anim
//            TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("multiplayer_progressanim").Visible = true;

//            // Wait until the session creation completes
//            while (!result.IsCompleted) yield return null;

//            // hide progress anim
//            TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("multiplayer_progressanim").Visible = false;

//            // get the session
//            NetworkSession session = null;

//            try
//            {
//                session = NetworkSessionManager.Instance.EndCreate(result);
//            }
//            catch (Exception e)
//            {
//                Assert.Warn(false, "Warning: EndCreate() threw an exception (usually because a network session already exists): " + e.Message);
//            }

//            // if we failed to create a session then let the player know
//            if (session == null)
//            {
//                Assert.Warn(false, "Warning: Could not create LIVE multiplayer match - BeginCreate() returned null");
//                DialogNotification.Show(DialogNotification.MESSAGE_COULDNOTCREATEMATCH, GUIActions.PopGameState);
//                yield break;
//            }

//            // else set the newly created session as the active one
//            NetworkSessionManager.Instance.Session = session;

//            // create the game settings object and add the host to the list of players
//            Game.Instance.ActiveGameplaySettings = new GamePlaySettingsMultiplayerLIVE(Game.Instance.LastLIVESettings);

//            foreach (SignedInGamer gamer in SignedInGamer.SignedInGamers)
//            {
//                if (gamer.Gamertag == session.Host.Gamertag)
//                {
//                    Game.Instance.ActiveGameplaySettings.AddPlayer(new PlayerLocal((int)gamer.PlayerIndex));
//                    break;
//                }
//            }

//            // let's all go to the lobby...
//            GameStateManager.Instance.Push(GameStateNames.SETTINGS_MULTIPLAYER_LIVE, null);
//        }
////#endif


//        public static GUIActionDelegate Play { get { return _play; } }

//        private static GUIActionDelegate _play = new GUIActionDelegate(delegate(GUIComponent guiComponent, string paramString)
//        {
//            Debug.WriteLine("(Action) pushing gamestate LIVE Multiplayer Settings");

////#if XBOX
//            Game.Instance.AddAsyncTask(AsyncTask_GotoSettingsLobbyLIVE(), true);
////#else
////            // PC version can't do multiplayer stuff
////            Debug.WriteLine("(Action) (PC) *not* pushing gamestate LIVE Multiplayer Settings");
////#endif
//        });
//    }
//}
