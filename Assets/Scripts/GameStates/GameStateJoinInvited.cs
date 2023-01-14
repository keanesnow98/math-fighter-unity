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
    /// This gamestate handles a player joining a game they were invited to join.
    /// </summary>
    [TorqueXmlSchemaType]
    public class GameStateJoinInvited : GameState
    {
        private static GameStateJoinInvited _this;
        private bool _isExiting;


        public override void Init()
        {
            base.Init();
            _this = this;
        }

        public override void PreTransitionOn(string paramString)
        {
            base.PreTransitionOn(paramString);
            Game.Instance.LoadScene(@"data\levels\JoinInvited.txscene");
        }

        public override void OnTransitionOnCompleted()
        {
            base.OnTransitionOnCompleted();
            Game.Instance.AddAsyncTask(AsyncTask_JoinInvitedSession(), true);
        }

        public override void OnTransitionOffCompleted()
        {
            base.OnTransitionOffCompleted();
            Game.Instance.UnloadScene(@"data\levels\JoinInvited.txscene");
        }

        /// <summary>
        /// This task will join the session we were invited to
        /// </summary>
        private IEnumerator<AsyncTaskStatus> AsyncTask_JoinInvitedSession()
        {
            // join the LIVE session
            IAsyncResult result;

            try
            {
                result = NetworkSessionManager.Instance.BeginJoinInvited(null, null);
            }
            catch (Exception e) // generic exception catching as we just want to abort if anything goes wrong
            {
                // something went really wrong - abort!
                _isExiting = true;
                Assert.Warn(false, "Warning: Could not join invited LIVE multiplayer match - BeginJoinInvited() threw an exception:\n" + e.Message);
                NetworkSessionManager.Instance.ShutdownSession();   // incase the error was due to already having a session active
                TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("joininvited_progressanim").Visible = false;
                DialogNotification.Show(DialogNotification.MESSAGE_COULDNOTJOINMATCH, _quitJoining);
                yield break;
            }

            // Wait until the joining process completes
            while (!result.IsCompleted && !_isExiting) yield return null;

            if (_isExiting)
            {
                NetworkSessionManager.Instance.ShutdownSession();
                TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("joininvited_progressanim").Visible = false;
                yield break;
            }

            try
            {
                // get the session we we have joined
                NetworkSession session = NetworkSessionManager.Instance.EndJoinInvited(result);
                NetworkSessionManager.Instance.Session = session;
            }
            catch (Exception e)
            {
                // we couldn't join the session
                _isExiting = true;
                Assert.Warn(false, "Warning: Could not join invited LIVE multiplayer match - EndJoinInvited() threw an exception:\n" + e.Message);
                NetworkSessionManager.Instance.ShutdownSession();   // incase the error was due to already having a session active
                TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("joininvited_progressanim").Visible = false;
                DialogNotification.Show(DialogNotification.MESSAGE_COULDNOTJOINMATCH, _quitJoining);
                yield break;
            }

            // create a game settings object for the gameplay to use
            Game.Instance.ActiveGameplaySettings = new GamePlaySettingsMultiplayerLIVE();

            // wait for the host to tell us which gamestate to goto
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
                NetworkSessionManager.Instance.ShutdownSession();
                TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("joininvited_progressanim").Visible = false;
                yield break;
            }

            if (!NetworkSessionManager.Instance.IsValidSession)
            {
                _isExiting = true;
                TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("joininvited_progressanim").Visible = false;
                NetworkSessionManager.Instance.ShutdownSession();
                DialogNotification.Show(DialogNotification.MESSAGE_COULDNOTJOINMATCH, _quitJoining);
                yield break;
            }

            // hide progress anim
            TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("joininvited_progressanim").Visible = false;

            // go to the gamestate that the game is currently in
            _isExiting = true;
            GameStateManager.Instance.Push(msg.Data as string, GameStateSettingsMultiPlayerLIVE.ISJOINING);
        }

        private void OnAction_Back()
        {
            if (!_isExiting)
            {
                _isExiting = true;

                // player needs to be able to quit out if they want to so we will go to the main menu
                GameStateManager.Instance.ClearAndLoad(GameStateNames.MAINMENU, null);
            }
        }



        // NOTE: not actually active at the moment - will activate it if needed later
        public static GUIActionDelegate Back { get { return _back; } }

        private static GUIActionDelegate _back = new GUIActionDelegate(delegate(GUIComponent guiComponent, string paramString)
        {
            Debug.WriteLine("(Action) quitting join-invited and going to main menu (if not already exiting the gamestate)");
            _this.OnAction_Back();
        });


        private static GUIActionDelegate _quitJoining = new GUIActionDelegate(delegate(GUIComponent guiComponent, string paramString)
        {
            Debug.WriteLine("(Action) quitting join-invited and going to main menu (if not already exiting the gamestate)");
            GameStateManager.Instance.ClearAndLoad(GameStateNames.MAINMENU, null);
        });
    }
}
