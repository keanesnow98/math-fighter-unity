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
using System.Diagnostics;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Net;
using MathFreak.AsyncTaskFramework;



namespace MathFreak.GameStates
{
    /// <summary>
    /// This gamestate handles the game settings screen for local multiplayer
    /// </summary>
    [TorqueXmlSchemaType]
    public class GameStateSettingsMultiPlayerLocal : GameStateSettingsMultiPlayer
    {
        protected override int MinPlayersRequired()
        {
            return 2;
        }

        protected override List<Player> GetPlayerList()
        {
            UpdatePlayersList();
            return Game.Instance.ActiveGameplaySettings.Players;
        }

        protected override void UpdateReadyStatuses()
        {
            GamerCollection<NetworkGamer> gamers = NetworkSessionManager.Instance.Session.AllGamers;
            int count = Game.Instance.ActiveGameplaySettings.Players.Count;

            for (int i = 0; i < count; i++)
            {
                foreach (NetworkGamer gamer in gamers)
                {
                    if (gamer.Gamertag == Game.Instance.ActiveGameplaySettings.Players[i].GamerTag)
                    {
                        _readyStatus[i] = gamer.IsReady;
                    }
                }
            }
        }

        protected override void OnGamerLeft(SessionEventInfo eventInfo)
        {
            Debug.WriteLine("Gamer left: " + (eventInfo.Args as GamerLeftEventArgs).Gamer.Gamertag);
            base.OnGamerLeft(eventInfo);
        }

        protected override void OnGamerJoined(SessionEventInfo eventInfo)
        {
            Debug.WriteLine("Gamer joined: " + (eventInfo.Args as GamerJoinedEventArgs).Gamer.Gamertag);
            base.OnGamerJoined(eventInfo);
        }

        protected override void OnSessionEnded(SessionEventInfo eventInfo)
        {
            // the session can only end if the main signed in gamer signs out,
            // and if that happens the game will return to the sign-in screen anyway.
            _isExiting = true;
        }

        protected override void GotoSelectionScreen()
        {
            base.GotoSelectionScreen();

            // store settings as 'most recently used' (these will be saved to disk when player returns to dashboard so that last settings are remembered)
            Game.Instance.LastLocalSettings = new GamePlaySettings(Game.Instance.ActiveGameplaySettings);
            Game.Instance.LastLocalSettings.RemoveAllPlayers();
            Game.Instance.LastLocalSettings.ResetScoreData();

#if XBOX
            Debug.WriteLine("(Action) pushing gamestate character selection");

            // clear all the player 'ready' statuses so they are not 'auto-ready' when we return to the lobby
            NetworkSessionManager.Instance.Session.ResetReady();

            GameStateManager.Instance.ClearAndLoad(GameStateNames.CHARACTERSELECTION_MULTIPLAYERLOCAL, null);
#else
            // PC version can't do multiplayer stuff
            Debug.WriteLine("(Action) (PC version) *not* pushing gamestate Lobby character selection");
#endif
        }

#if XBOX
        /// <summary>
        /// Will prompt for players to sign-in.  If not enough players sign-in then it will show a
        /// dialog asking whether to quit to the main menu or not.
        /// </summary>
        private IEnumerator<AsyncTaskStatus> AsyncTask_SigninLocalPlayers()
        {
            if (_isExiting) yield break;    // don't show the dialog if we are exiting (it would keep popping up unless someone signs in so very annoying for the player and would take ages to exit the gamestate potentially)

            if (IsWaiting("signinlocalplayers")) yield break;   // it's possible for this task to get triggered when it's already running, so ignore any extra triggering as the task is already running.

            RegisterWaiting("signinlocalplayers");

            // if the guide is already showing then wait until it disappears before we try and show the sign-in dialog
            while (Guide.IsVisible) yield return null;

            // allow players to sign in
            Guide.ShowSignIn(2, false);

            // wait for the sign-in dialog to disappear
            while (Guide.IsVisible) yield return null;

            // if enough players still aren't signed in then quit this task
            if (SignedInGamer.SignedInGamers.Count < 2)
            {
                Debug.WriteLine("multiplayer local match - ShowSignIn() - not enough players are signed in");
                AddAsyncTask((_activeInstance as GameStateSettingsMultiPlayerLocal).AsyncTask_ShowQuitDialog(), true);
                UnRegisterWaiting("signinlocalplayers");
                yield break;
            }

            // else update the player list and we are good to go
            UpdatePlayers();

            UnRegisterWaiting("signinlocalplayers");
        }
#endif

        private void UpdatePlayersList()
        {
            // first, clear the players list
            Game.Instance.ActiveGameplaySettings.RemoveAllPlayers();

#if XBOX
            // then add the main signed in player
            foreach (SignedInGamer gamer in SignedInGamer.SignedInGamers)
            {
                if (gamer.Gamertag == Game.Instance.Gamertag)
                {
                    Game.Instance.ActiveGameplaySettings.AddPlayer(new PlayerLocal((int)(gamer.PlayerIndex)));
                    break;
                }
            }

            // then add the first other signed in player
            foreach (SignedInGamer gamer in SignedInGamer.SignedInGamers)
            {
                if (gamer.Gamertag != Game.Instance.Gamertag)
                {
                    Game.Instance.ActiveGameplaySettings.AddPlayer(new PlayerLocal((int)(gamer.PlayerIndex)));

                    // also add the gamer to the session if they aren't already in it
                    bool isInSession = false;

                    foreach (NetworkGamer networkGamer in NetworkSessionManager.Instance.Session.AllGamers)
                    {
                        if (networkGamer.Gamertag == gamer.Gamertag)
                        {
                            isInSession = true;
                            break;
                        }
                    }

                    if (!isInSession)
                    {
                        NetworkSessionManager.Instance.Session.AddLocalGamer(gamer);
                    }

                    break;
                }
            }

            // if less than two players then we need to prompt for more
            if (Game.Instance.ActiveGameplaySettings.Players.Count < 2)
            {
                if (!_isShowingQuitDialog)  // don't prompt again yet - player is deciding whether to quit or retry joining local multiplayer session
                {
                    AddAsyncTask(AsyncTask_SigninLocalPlayers(), true);
                }
            }
            else
            {
                AddAsyncTask(AsyncTask_WaitForGamerListUpdate(Game.Instance.ActiveGameplaySettings.Players.Count), true);
            }
#endif
        }

        private IEnumerator<AsyncTaskStatus> AsyncTask_WaitForGamerListUpdate(int targetCount)        
        {
            if (IsWaiting("waitforgamerlistupdate")) yield return null;    // already got this task running so no need to run another one

            RegisterWaiting("waitforgamerlistupdate");

            while (NetworkSessionManager.Instance.Session.AllGamers.Count != targetCount)
            {
                Debug.WriteLine("waiting for gamerlist to update");
                yield return null;
            }

            UnRegisterWaiting("waitforgamerlistupdate");
        }
    }
}
