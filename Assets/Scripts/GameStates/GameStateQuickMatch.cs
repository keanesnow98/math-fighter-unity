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
using Microsoft.Xna.Framework.Graphics;
using MathFreak.Text;
using MathFreak.GameStates.Dialogs;



namespace MathFreak.GameStates
{
    /// <summary>
    /// This gamestate automatically picks a match from the matches found and attempts to join it.
    /// Will not pick matches that are already full and will show a 'no games found' message if
    /// no games were found.
    /// </summary>
    [TorqueXmlSchemaType]
    public class GameStateQuickMatch : GameState
    {
        private static GameStateQuickMatch _this;
        private bool _isExiting;

        
        public override void Init()
        {
            base.Init();
            _this = this;
        }

        public override void PreTransitionOn(string paramString)
        {
            base.PreTransitionOn(paramString);

            _isExiting = false;

            Game.Instance.LoadScene(@"data\levels\QuickMatch.txscene");
        }

        public override void OnTransitionOnCompleted()
        {
            base.OnTransitionOnCompleted();

            // try to find a session and try to join it
            Game.Instance.AddAsyncTask(AsyncTask_FindAndJoinSession(), true);

            TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("quickmatch_progressanim").Visible = true;

            GUIManager.Instance.ActivateGUI();
        }

        public override void PreTransitionOff()
        {
            base.OnTransitionOffCompleted();
            Game.Instance.UnloadScene(@"data\levels\QuickMatch.txscene");
        }

        /// <summary>
        /// Will look for available sessions - if any are found then will attempt to join one of them.
        /// </summary>
        private IEnumerator<AsyncTaskStatus> AsyncTask_FindAndJoinSession()
        {
            if (_isExiting) yield break;

            // Start the async session finding
            IAsyncResult result;
            AvailableNetworkSessionCollection availableSessions;

            yield return null;  // transition on has completed for this gamestate, but we need to yield for one tick incase we need to immediately show a notification dialog - would crash due to being technically still in the middle of transitioning the quick match gamestate

            try
            {
                result = NetworkSessionManager.Instance.BeginFind(NetworkSessionManager.PLAYERMATCH, 1, GamePlaySettings.GetSessionPropertiesPattern(), null, null);
            }
            catch (Exception e) // generic exception catching as we just want to abort if anything goes wrong
            {
                // something went really wrong - abort!
                _isExiting = true;
                Assert.Warn(false, "Warning: Could not find any LIVE multiplayer matches - BeginFind() threw an exception:\n" + e.Message);
                NetworkSessionManager.Instance.ShutdownSession();   // incase the error was due to already having a session active
                TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("quickmatch_progressanim").Visible = false;
                DialogNotification.Show(DialogNotification.MESSAGE_NOGAMESFOUND, _quitQuickMatchScreen);
                yield break;
            }

            // Wait until the find operation has completed
            while (!result.IsCompleted && !_isExiting) yield return null;

            if (_isExiting)
            {
                NetworkSessionManager.Instance.ShutdownSession();
                TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("quickmatch_progressanim").Visible = false;
                yield break;
            }

            // get the list of available sessions
            try
            {
                availableSessions = NetworkSessionManager.Instance.EndFind(result);
            }
            catch (Exception e) // EndFind() is not documented as throwing any exceptions but it does!!!
            {
                Assert.Warn(false, "Warning: could not get available sessions list - EndFind() threw an exception:\n" + e.Message);
                NetworkSessionManager.Instance.ShutdownSession();   // incase the error was due to already having a session active
                availableSessions = null;
            }

            // if we didn't find any sessions then quit this gamestate
            if (availableSessions == null || availableSessions.Count == 0)
            {
                _isExiting = true;
                TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("quickmatch_progressanim").Visible = false;
                DialogNotification.Show(DialogNotification.MESSAGE_NOGAMESFOUND, _quitQuickMatchScreen);
                yield break;
            }

            // filter the sessions list for games with 2 or fewer players
            List<AvailableNetworkSession> sessionsList = new List<AvailableNetworkSession>(availableSessions.Count);

            for (int i = 0; i < availableSessions.Count; i++)
            {
                if (availableSessions[i].CurrentGamerCount <= 2) sessionsList.Add(availableSessions[i]);
            }

            // if no session to join then quit this gamestate
            if (sessionsList.Count == 0)
            {
                _isExiting = true;
                TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("quickmatch_progressanim").Visible = false;
                DialogNotification.Show(DialogNotification.MESSAGE_NOGAMESFOUND, _quitQuickMatchScreen);
                yield break;
            }

            // else time to join a session - pick one at random and join it
            AvailableNetworkSession sessionToJoin = sessionsList[Game.Instance.Rnd.Next(sessionsList.Count)];

            try
            {
                result = NetworkSessionManager.Instance.BeginJoin(sessionToJoin, null, null);
            }
            catch (Exception e) // generic exception catching as we just want to abort if anything goes wrong
            {
                // something went really wrong - abort!
                _isExiting = true;
                Assert.Warn(false, "Warning: Could not join LIVE multiplayer match - BeginJoin() threw an exception:\n" + e.Message);
                NetworkSessionManager.Instance.ShutdownSession();   // incase the error was due to already having a session active
                TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("quickmatch_progressanim").Visible = false;
                DialogNotification.Show(DialogNotification.MESSAGE_COULDNOTJOINMATCH, _quitQuickMatchScreen);
                yield break;
            }

            // Wait until the joining process completes
            while (!result.IsCompleted && !_isExiting) yield return null;

            if (_isExiting)
            {
                NetworkSessionManager.Instance.ShutdownSession();
                TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("quickmatch_progressanim").Visible = false;
                yield break;
            }

            try
            {
                // get the session we we have joined
                NetworkSession session = NetworkSessionManager.Instance.EndJoin(result);
                NetworkSessionManager.Instance.Session = session;
            }
            catch (Exception e)
            {
                // we couldn't join the session
                _isExiting = true;
                Assert.Warn(false, "Warning: Could not join LIVE multiplayer match - EndJoin() threw an exception:\n" + e.Message);
                NetworkSessionManager.Instance.ShutdownSession();
                TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("quickmatch_progressanim").Visible = false;
                DialogNotification.Show(DialogNotification.MESSAGE_COULDNOTJOINMATCH, _quitQuickMatchScreen);
                yield break;
            }

            // create a game settings object for the gameplay to use
            Game.Instance.ActiveGameplaySettings = new GamePlaySettingsMultiplayerLIVE();

            // wait for the host to tell us which gamestate to go to
            LocalNetworkGamer localGamer = NetworkSessionManager.Instance.Session.LocalGamers[0];
            NetworkMessage msg = new NetworkMessage();

            while (msg.Type != NetworkMessage.EnumType.GameStateJoined && !_isExiting && NetworkSessionManager.Instance.IsValidSession)
            {
                if (localGamer.IsDataAvailable)
                {
                    NetworkSessionManager.Instance.RecieveDataFromHost(msg, localGamer);
                }

                yield return null;
            }

            if (_isExiting)
            {
                TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("quickmatch_progressanim").Visible = false;
                NetworkSessionManager.Instance.ShutdownSession();
                yield break;
            }

            if (!NetworkSessionManager.Instance.IsValidSession)
            {
                _isExiting = true;
                TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("quickmatch_progressanim").Visible = false;
                NetworkSessionManager.Instance.ShutdownSession();
                DialogNotification.Show(DialogNotification.MESSAGE_COULDNOTJOINMATCH, _quitQuickMatchScreen);
                yield break;
            }

            // hide progress anim
            TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("quickmatch_progressanim").Visible = false;

            // goto the gamestate that the game is currently in
            GameStateManager.Instance.Push(msg.Data as string, GameStateSettingsMultiPlayerLIVE.ISJOINING);
        }

        private void OnAction_Back()
        {
            if (!_isExiting)
            {
                _isExiting = true;

                GameStateManager.Instance.ClearAndLoadWithPrefilledStack(GameStateNames.XBOXLIVE, new string[] { GameStateNames.MAINMENU, GameStateNames.MULTIPLAYER }, null);
            }
        }



        public static GUIActionDelegate Back { get { return _back; } }

        private static GUIActionDelegate _back = new GUIActionDelegate(delegate(GUIComponent guiComponent, string paramString)
        {
            Debug.WriteLine("(Action) quitting quick-match and going to main menu (if not already exiting the gamestate)");
            _this.OnAction_Back();
        });



        private static GUIActionDelegate _quitQuickMatchScreen = new GUIActionDelegate(delegate(GUIComponent guiComponent, string paramString)
        {
            Debug.WriteLine("(Action) quitting the quick match screen");
            GameStateManager.Instance.ClearAndLoadWithPrefilledStack(GameStateNames.XBOXLIVE, new string[] { GameStateNames.MAINMENU, GameStateNames.MULTIPLAYER }, null);
        });
    }
}
