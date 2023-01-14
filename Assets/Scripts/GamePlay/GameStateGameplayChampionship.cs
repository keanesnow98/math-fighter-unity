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
using Microsoft.Xna.Framework.Net;
using System.Diagnostics;
using MathFreak.GameStates;
using MathFreak.GameStates.Dialogs;
using MathFreak.AsyncTaskFramework;
using MathFreak.Text;
using Microsoft.Xna.Framework.GamerServices;
using MathFreak.Highscores;



namespace MathFreak.GamePlay
{
    /// <summary>
    /// This state handles the championship mode specific stuff
    /// </summary>
    public class GameStateGameplayChampionship : GameStateGameplayMultiplayer
    {
        private bool _wasChallenged;


        public override void OnTransitionOnCompleted()
        {
            base.OnTransitionOnCompleted();

            // if we are accepting challengers then set that stuff up
            if (Game.Instance.ActiveGameplaySettings.AllowChallengers)
            {
                AddAsyncTask(AsyncTask_JoinOrCreateNetworkSession(), true);
            }

            //// TESTING - try to break the p2p stuff by modifying the highscore table while p2p is active
            //HighscoreData.Instance.AddSinglePlayerHighscore(new HighscoreData.SinglePlayerScoreData("test", 1000, 1));
        }

        protected override void InitializeGamePlay()
        {
            base.InitializeGamePlay();

            _wasChallenged = false;
            _playerScoreText.Visible = true;
        }

        protected override void ProcessQuestionDisplay()
        {
            base.ProcessQuestionDisplay();
            
            (Game.Instance.ActiveGameplaySettings.Players[1] as PlayerLocalAI).OnQuestionStarted(_gameplayData.Question.RightAnswer, _gameplayData.Question.AIMinTime, _gameplayData.Question.AIMaxTime, _gameplayData.Question.AIAvgTime, _health[0], _multiplier);
        }

        protected override void ProcessQuestionOutro()
        {
            (Game.Instance.ActiveGameplaySettings.Players[1] as PlayerLocalAI).OnQuestionFinished();

            base.ProcessQuestionOutro();
        }

        protected override void ProcessState()
        {
            (Game.Instance.ActiveGameplaySettings.Players[1] as PlayerLocalAI).Tick(_dt);

            base.ProcessState();
        }

        protected override void ProcessOutro()
        {
            //if (_isExiting) return;

            //_isExiting = true;

            OnPreExiting();

            // if player lost (or a draw) then show a dialog prompting to retry
            if (_health[0] == 0)
            {
                AddAsyncTask(AsyncTask_ShowReplayDialog(), true);
            }
            // else move to the selection screen
            else
            {
                Game.Instance.ActiveGameplaySettings.AccumulateScoreData(_playerScore, _health);

                PlayerLocalAI aiPlayer = (Game.Instance.ActiveGameplaySettings.Players[1] as PlayerLocalAI);
                aiPlayer.OnLost();

                // goto the appropriate screen depending on whether player has won the championship or there are more characters to defeat
                OnExiting();

                if (aiPlayer.IsDefeated)
                {
                    Game.Instance.ActiveGameplaySettings.StoreScoreData();
                    HighscoreData.Instance.Save();
                    GameStateManager.Instance.ClearAndLoad(GameStateNames.WON_CHAMPIONSHIP, null);
                }
                else
                {
                    GameStateManager.Instance.ClearAndLoadWithPrefilledStack(GameStateNames.CHARACTERSELECTION_SINGLEPLAYER, new string[] { GameStateNames.MAINMENU, GameStateNames.SETTINGS_SINGLEPLAYER }, null);
                }
            }
        }

        public override void OnQuitting()
        {
            //Game.Instance.ActiveGameplaySettings.StoreScoreData();  // save whatever scoring data the player has achieved so far
            base.OnQuitting();
        }

        protected override void OnPreExiting()
        {
            if (Game.Instance.ActiveGameplaySettings.AllowChallengers && !_wasChallenged)
            {
                NetworkSessionManager.Instance.ShutdownSession();
                GameStateVsSplashSinglePlayer.ClearSavedProgress(); // make sure any saved championship progress is cleared (we weren't challenged so we don't need it)
            }

            base.OnPreExiting();
        }

        protected override void DoUnload()
        {
            if (Game.Instance.ActiveGameplaySettings.AllowChallengers && !_wasChallenged)
            {
                NetworkSessionManager.Instance.ShutdownSession();
            }

            base.DoUnload();
        }

        protected IEnumerator<AsyncTaskStatus> AsyncTask_ShowReplayDialog()
        {
            // block non-async gameplay state processing
            // NOTE: we won't unregister the wait request as nothing else should happen as regards gameplay processing now anyway and it's best to stop anything happening that might mess up exiting the gameplay gamestate properly
            RegisterWaiting("replaymatch");

            // show the dialog
            DialogReplayChampionshipMatch.ResetResponse();
            GameStateManager.Instance.PushOverlay(GameStateNames.DIALOG_REPLAYCHAMPIONSHIPMATCH, null);

            // wait for a response from the user
            while (!DialogReplayChampionshipMatch.ResponseRecieved) yield return null;

            // if player said 'yes' the reload the match - via the Vs Screen
            if (DialogReplayChampionshipMatch.Response)
            {
                yield return null;  // allow an extra tick so the dialog is *definitely* finished unloading itself before we unload ourselves too - or things could get messed up.
                Debug.WriteLine("replaying!");
                Game.Instance.ActiveGameplaySettings.HasReplayed = true;
                Game.Instance.ActiveGameplaySettings.ResetScoreData();
                UnloadAssets();
                GameStateManager.Instance.Push(GameStateNames.VS_SPLASH_SINGLEPLAYER, null);
            }
            // else quit to the main menu
            else
            {
                Debug.WriteLine("NOT replaying!");
                Game.Instance.ActiveGameplaySettings.StoreScoreData();
                HighscoreData.Instance.Save();
                UnloadAssets();
                GameStateManager.Instance.ClearAndLoad(GameStateNames.MAINMENU, GameStateMainMenu.RETURNING_FROM_GAME);
            }
        }

        protected override string GetGameOverMessage()
        {
            if (_health[1] == 0)
            {
                return "YOU WIN!";
            }
            else
            {
                return "YOU LOSE!";
            }
        }

        protected override Game.EnumMathFreakFont GetGameOverMessageFont()
        {
            return Game.EnumMathFreakFont.PlayerXXXXWins;
        }

        protected override void PlayWinLoseSFX()
        {
            if (_health[1] == 0)
            {
                MFSoundManager.Instance.PlaySFX(MFSoundManager.EnumSFX.YouWin);
            }
            else
            {
                MFSoundManager.Instance.PlaySFX(MFSoundManager.EnumSFX.YouLose);
            }
        }

        // This task will attempt to find a LIVE game to join.  If no game can be found then
        // a LIVE network session will be created and we will wait for someone to challenge us.
        private IEnumerator<AsyncTaskStatus> AsyncTask_JoinOrCreateNetworkSession()
        {
            if (_isExiting) yield break;

            //////////////////////////////////////////////////
            // PART 1 - Try and find a game to join
            //////////////////////////////////////////////////
            AvailableNetworkSessionCollection availableSessions = null;
            bool searchFailed = false;
            IAsyncResult result = null;

            try
            {
                Debug.WriteLine("SP trying to auto find a game");
                result = NetworkSessionManager.Instance.BeginFind(NetworkSessionManager.PLAYERMATCH, 1, GamePlaySettings.GetSessionPropertiesPattern(), null, null);
            }
            catch (Exception e) // generic exception catching as we just want to abort if anything goes wrong
            {
                // something went really wrong - abort!
                Assert.Warn(false, "Warning: Could not find any LIVE multiplayer matches - BeginFind() threw an exception:\n" + e.Message);
                NetworkSessionManager.Instance.ShutdownSession();   // incase the error was due to already having a session active
                availableSessions = null;
                searchFailed = true;
            }

            if (_isExiting) yield break;

            if (!searchFailed)
            {
                while (!result.IsCompleted && !_isExiting) yield return null;

                if (_isExiting) yield break;

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
            }

            if (_isExiting) yield break;

            if (availableSessions != null)
            {
                Debug.WriteLine("SP trying to join auto found games");

                AvailableNetworkSession session;

                // go through the list trying to join one
                for (int i = 0; i < availableSessions.Count; i++)
                {
                    if (_isExiting) yield break;

                    bool joinFailed = false;
                    session = availableSessions[i];

                    // only try and auto join if there is a single player in the session
                    if (session.CurrentGamerCount == 1)
                    {
                        try
                        {
                            Debug.WriteLine("SP trying to auto join a game");
                            result = NetworkSessionManager.Instance.BeginJoin(session, null, null);
                        }
                        catch (Exception e) // generic exception catching as we just want to abort if anything goes wrong
                        {
                            // something went really wrong - abort!
                            Assert.Warn(false, "Warning: Could not join LIVE multiplayer match - BeginJoin() threw an exception:\n" + e.Message);
                            NetworkSessionManager.Instance.ShutdownSession();   // incase the error was due to already having a session active
                            joinFailed = true;
                        }

                        if (!joinFailed)
                        {
                            // wait for join process to complete
                            while (!result.IsCompleted && !_isExiting) yield return null;

                            if (_isExiting) yield break;

                            try
                            {
                                NetworkSession joinedSession = NetworkSessionManager.Instance.EndJoin(result);
                                NetworkSessionManager.Instance.Session = joinedSession;
                            }
                            catch (Exception e)
                            {
                                // we couldn't join the session
                                Assert.Warn(false, "Warning: Could not join LIVE multiplayer match - EndJoin() threw an exception:\n" + e.Message);
                                NetworkSessionManager.Instance.ShutdownSession();   // incase the error was due to already having a session active
                                joinFailed = true;
                            }
                        }

                        if (!joinFailed)
                        {
                            GameStateManager.Instance.PopOverlaysImmediately();
                            KillAllTasks(false, true);
                            AddAsyncTask(AsyncTask_GotoSettingsLobbyAsClient(), true);
                            yield break;    // we're done - exit this task now
                        }
                    }

                    yield return null;
                }
            }

            if (_isExiting) yield break;

            //////////////////////////////////////////////////
            // PART 2 - We couldn't find a session to join so
            // we'll create our own and wait for someone to
            // challenge us.
            //////////////////////////////////////////////////
            try
            {
                Debug.WriteLine("SP trying to auto create a session");
                result = NetworkSessionManager.Instance.BeginCreate(NetworkSessionManager.PLAYERMATCH, 1, 2, 0, GamePlaySettings.GetInitialSessionCreationProperties(), null, null);
            }
            catch (Exception e) // generic exception catching as we just want to abort if anything goes wrong
            {
                // something went really wrong - abort!
                Assert.Warn(false, "Warning: Could not create LIVE multiplayer match - BeginCreate() threw an exception:\n" + e.Message);
                NetworkSessionManager.Instance.ShutdownSession();   // incase the error was due to already having a session active
                yield break;
            }

            // Wait until the session creation completes
            while (!result.IsCompleted && !_isExiting) yield return null;

            if (_isExiting) yield break;

            // get the session
            NetworkSession createdSession = null;

            try
            {
                createdSession = NetworkSessionManager.Instance.EndCreate(result);
            }
            catch (Exception e)
            {
                Assert.Warn(false, "Warning: EndCreate() threw an exception (usually because a network session already exists): " + e.Message);
                NetworkSessionManager.Instance.ShutdownSession();   // incase the error was due to already having a session active
            }

            // if we failed to create a session then exit
            if (createdSession == null)
            {
                Assert.Warn(false, "Warning: Could not create LIVE multiplayer match - BeginCreate() returned null");
                NetworkSessionManager.Instance.ShutdownSession();   // incase the error was due to already having a session active
                yield break;
            }

            Debug.WriteLine("SP created a session and waiting for other players to join");

            // else set the newly created session as the active one
            NetworkSessionManager.Instance.Session = createdSession;
        }

        // **All we have to do here is show a message and then go to the lobby**  The lobby itself
        // will do the actual join-handling stuff and anything else just like it would in a normal
        // game joining situation.
        private IEnumerator<AsyncTaskStatus> AsyncTask_GotoSettingsLobbyAsHost()
        {
            if (_isExiting) yield break;

            _isExiting = true;

            RegisterWaiting("newchallenger");

            _wasChallenged = true;

            // disable the GUI
            GUIManager.Instance.DeactivateGUI();

            // disable the pause menu
            _canShowPauseMenu = false;

            // show our message
            T2DSceneObject messageObj = TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("hud_messagetext");
            MathMultiTextComponent messageText = messageObj.Components.FindComponent<MathMultiTextComponent>();
            messageObj.Visible = true;
            messageObj.Position = Vector2.Zero;
            messageText.Font = Game.EnumMathFreakFont.PlayerXXXXWins;
            messageText.TextValue = "New Challenger!!!";

            // play the sfx
            MFSoundManager.Instance.PlaySFX(MFSoundManager.EnumSFX.NewChallenger);

            // wait for a bit - allow time for player to read the message and for the sfx to finish playing
            float elapsed = 0.0f;

            while (elapsed < 2.0f)
            {
                elapsed += _dt;
                yield return null;
            }

            // create a gamesettings instance for the new game
            Game.Instance.ActiveGameplaySettings = new GamePlaySettingsMultiplayerLIVE();

            // if session is no longer valid then abort - don't worry about returning to the game - if
            // the session ended then we will handle the that event in the lobby.
            if (!NetworkSessionManager.Instance.IsValidSession)
            {
                // go to the lobby - which will handle the session end event.
                // ...in the meantime we still need to set up a few things before heading to the lobby so the lobby will load properly
                UnloadAssets();
                GameStateManager.Instance.ClearAndLoadWithPrefilledStack(GameStateNames.SETTINGS_MULTIPLAYER_LIVE, new string[] { GameStateNames.MAINMENU, GameStateNames.MULTIPLAYER }, GameStateSettingsMultiPlayerLIVE.WAS_CHALLENGED);
                yield break;
            }

            // setup the match and goto the lobby
            // ...create the game settings object and add the host to the list of players
            // ...(the lobby will handle the joined event to add other players to the game)
            foreach (SignedInGamer gamer in SignedInGamer.SignedInGamers)
            {
                if (gamer.Gamertag == NetworkSessionManager.Instance.Session.Host.Gamertag)
                {
                    Game.Instance.ActiveGameplaySettings.AddPlayer(new PlayerLocal((int)gamer.PlayerIndex));
                    break;
                }
            }

            // ...clear and load the settings-lobby screen with the appropriate screens pushed under it on the stack
            UnloadAssets();
            GameStateManager.Instance.ClearAndLoadWithPrefilledStack(GameStateNames.SETTINGS_MULTIPLAYER_LIVE, new string[] { GameStateNames.MAINMENU, GameStateNames.MULTIPLAYER }, GameStateSettingsMultiPlayerLIVE.WAS_CHALLENGED);

            // NOTE: we don't unregister waiting as nothing should process anyway - we are leaving this game shortly
            //UnRegisterWaiting("newchallenger");
        }

        // Displays a message and then goes and joins the game (in whatever state it is currently in - might have left the lobby by the time we joined so we can't assume what state we need to join)
        private IEnumerator<AsyncTaskStatus> AsyncTask_GotoSettingsLobbyAsClient()
        {
            if (_isExiting) yield break;

            _isExiting = true;

            RegisterWaiting("autochallenging");

            _wasChallenged = true;

            // disable the GUI
            GUIManager.Instance.DeactivateGUI();

            // disable the pause menu
            _canShowPauseMenu = false;

            // show our message
            T2DSceneObject messageObj = TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("hud_messagetext");
            MathMultiTextComponent messageText = messageObj.Components.FindComponent<MathMultiTextComponent>();
            messageObj.Visible = true;
            messageObj.Position = Vector2.Zero;
            messageText.Font = Game.EnumMathFreakFont.PlayerXXXXWins;
            messageText.TextValue = "Auto Challenging!!!";

            // wait for a bit
            float elapsed = 0.0f;

            while (elapsed < 1.0f)
            {
                elapsed += _dt;
                yield return null;
            }

            // create a game settings object for the gameplay to use
            Game.Instance.ActiveGameplaySettings = new GamePlaySettingsMultiplayerLIVE();

            // wait for the host to tell us which gamestate to goto
            LocalNetworkGamer localGamer = NetworkSessionManager.Instance.Session.LocalGamers[0];
            NetworkMessage msg = new NetworkMessage();

            while (true)
            {
                // if session is no longer valid then abort - we'll go to the lobby as a client
                // and the lobby will handle the session ended notification (and give us the option
                // to return to our championship game of course).
                if (!NetworkSessionManager.Instance.IsValidSession)
                {
                    // go to the lobby - which will handle the session end event.
                    // ...in the meantime we still need to set up a few things before heading to the lobby so the lobby will load properly
                    UnloadAssets();
                    GameStateManager.Instance.ClearAndLoadWithPrefilledStack(GameStateNames.SETTINGS_MULTIPLAYER_LIVE, new string[] { GameStateNames.MAINMENU, GameStateNames.MULTIPLAYER }, GameStateSettingsMultiPlayerLIVE.WAS_CHALLENGED);
                    yield break;
                }

                // else check for the join-message that we are expecting
                if (localGamer.IsDataAvailable)
                {
                    NetworkSessionManager.Instance.RecieveDataFromHost(msg, localGamer);
                }

                // if we have recieved the message then we can go join the game proper
                if (msg.Type == NetworkMessage.EnumType.GameStateJoined)
                {
                    GameStateManager.Instance.Push(msg.Data as string, GameStateSettingsMultiPlayerLIVE.ISJOINING);
                    yield break;
                }

                yield return null;
            }

            // NOTE: we don't unregister waiting as nothing should process anyway - we are leaving this game shortly
            //UnRegisterWaiting("autochallenging");
        }

        protected override void OnGamerJoined(SessionEventInfo eventInfo)
        {
            Assert.Fatal(Game.Instance.ActiveGameplaySettings.AllowChallengers, "Processing network event when allow challengers is not enabled!");

            // check the session is valid
            if (!NetworkSessionManager.Instance.IsValidSession) return;

            if (_isExiting) return;

            base.OnGamerJoined(eventInfo);

            NetworkGamer gamer = (eventInfo.Args as GamerJoinedEventArgs).Gamer;

            // if the someone that joined is not us then we just got challenged so go
            // to the lobby and meet the challenger (note: there will be a gamer joined
            // message when we ourselves first start up the game so we need to ignore that)
            if (gamer.Gamertag != Game.Instance.Gamertag)
            {
                Debug.WriteLine("Challenged by gamer: " + gamer.Gamertag);
                GameStateManager.Instance.PopOverlaysImmediately();
                KillAllTasks(true, true);
                NetworkSessionManager.Instance.PushBackEvent(eventInfo);    // push the event back on the queue - the lobby will handle the gamer joining stuff for real when we get there
                AddAsyncTask(AsyncTask_GotoSettingsLobbyAsHost(), true);
            }
        }

        protected override void ProcessSessionEvents()
        {
            // only process events if we are allowing challengers
            // and if we are not already in the process of responding to a challenge (once we are responding to a challenge then we are definitely going to the lobby regardless of what events occur - so for consistency we will let the lobby handle any events)
            if (Game.Instance.ActiveGameplaySettings.AllowChallengers && !_wasChallenged)
            {
                base.ProcessSessionEvents();
            }
        }
    }
}
