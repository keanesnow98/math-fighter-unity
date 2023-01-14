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
using MathFreak.Math.Categories;
using MathFreak.AsyncTaskFramework;
using MathFreak.GameStates.Dialogs;
using System.Diagnostics;
using MathFreak.GameStates;
using Microsoft.Xna.Framework.GamerServices;
using MathFreak.Highscores;



namespace MathFreak.GamePlay
{
    /// <summary>
    /// This state handles the multiplayer gameplay stuff that is specific to a multiplayer game over
    /// xbox live
    /// </summary>
    public class GameStateGameplayMultiplayerLIVE : GameStateGameplayMultiplayer
    {
        private NetworkMessage _networkMessage;
        private static GameStateGameplayMultiplayerLIVE _this;

        private enum EnumJoiningState { WaitingForWelcomePack, WaitingToWatch, Joined };
        private EnumJoiningState _joiningState;

        private List<bool> _readyForQuestionContentCount = new List<bool>(2);


        public override void Init()
        {
            base.Init();

            _this = this;
            _networkMessage = new NetworkMessage();
        }

        public override void OnTransitionOnCompleted()
        {
            // if joining match midway through then need to get the welcome pack before we can properly sync with the game
            if (ParamString == GameStateSettingsMultiPlayerLIVE.ISJOINING)
            {
                _joiningState = EnumJoiningState.WaitingForWelcomePack;
            }
            // else already joined before we got to this gamestate so continue as usual
            else
            {
                _joiningState = EnumJoiningState.Joined;
            }

            if (_joiningState == EnumJoiningState.Joined)
            {
                base.OnTransitionOnCompleted();
            }
            else
            {
                LoadJoiningScene();
            }
        }

        private void LoadJoiningScene()
        {
            Game.Instance.LoadScene(@"data\levels\JoiningGameplayLIVE.txscene");
        }

        private void UnloadJoiningScene()
        {
            Game.Instance.UnloadScene(@"data\levels\JoiningGameplayLIVE.txscene");
        }

        public override void UnloadedImmediately()
        {
            base.UnloadedImmediately();

            // make sure any network session is shutdown
            NetworkSessionManager.Instance.ShutdownSession();
        }

        public override void OnQuitting()
        {
            if (!_isExiting)
            {
                if (GameStateVsSplashSinglePlayer.HasSavedProgress)
                {
                    NetworkSessionManager.Instance.ShutdownSession();
                    GameStateManager.Instance.PopOverlaysImmediately();
                    DialogNotification.Show(DialogNotification.MESSAGE_RETURNINGTOCHAMPIONSHIP, _returnToChampionship);
                }
                else
                {
                    NetworkSessionManager.Instance.ShutdownSession();
                    base.OnQuitting();
                }
            }
        }

        protected override void ProcessState()
        {
            // if we've already joined then do the usual processing
            // else we need to do processing specific to the requirements of joining the match midway through
            switch (_joiningState)
            {
                case EnumJoiningState.Joined:
                    base.ProcessState();
                    break;

                case EnumJoiningState.WaitingForWelcomePack:
                    ProcessWaitingForWelcomePack();
                    break;

                case EnumJoiningState.WaitingToWatch:
                    ProcessWaitingToWatch();
                    break;
            }
        }

        protected override void ProcessSessionEvents()
        {
            // only process events if we have joined and are in sync properly.
            // once we are in sync then we can process the events in the queue.
            // ...the exception is if the session has ended - in which case any
            // ...other messages and events are a moot point anyway (but we would
            // ...still need to process the session ended event to show the player
            // ...the notification).
            if (_joiningState == EnumJoiningState.Joined || !NetworkSessionManager.Instance.IsValidSession)
            {
                base.ProcessSessionEvents();
            }
        }

        protected override void ProcessOutro()
        {
            //if (_isExiting) return;

            //_isExiting = true;

            // update the player queue based on who won/lost
            if (_health[0] == 0)
            {
                (Game.Instance.ActiveGameplaySettings as GamePlaySettingsMultiplayerLIVE).UpdatePlayerQueueAfterMatch(Game.Instance.ActiveGameplaySettings.Players[0], Game.Instance.ActiveGameplaySettings.Players[1]);
            }
            else
            {
                (Game.Instance.ActiveGameplaySettings as GamePlaySettingsMultiplayerLIVE).UpdatePlayerQueueAfterMatch(Game.Instance.ActiveGameplaySettings.Players[1], Game.Instance.ActiveGameplaySettings.Players[0]);
            }
            
            // update scores
            Game.Instance.ActiveGameplaySettings.AccumulateScoreData(_health);
            Game.Instance.ActiveGameplaySettings.StoreScoreData();
            HighscoreData.Instance.Save();

            // clear and load the settings-lobby screen with the appropriate screens pushed under it on the stack
            UnloadAssets();

            if (!NetworkSessionManager.Instance.IsValidSession)
            {
                GameStateManager.Instance.ClearAndLoad(GameStateNames.MAINMENU, GameStateMainMenu.RETURNING_FROM_GAME);
            }
            else
            {
                GameStateManager.Instance.ClearAndLoadWithPrefilledStack(GameStateNames.SETTINGS_MULTIPLAYER_LIVE, new string[] { GameStateNames.MAINMENU, GameStateNames.MULTIPLAYER }, null);
            }
        }

        protected override void SetupPlayerInput()
        {
            Assert.Fatal(Game.Instance.ActiveGameplaySettings.Players.Count == 2, "Trying to play LIVE multiplayer with not enough players! - count is: " + Game.Instance.ActiveGameplaySettings.Players.Count);

            base.SetupPlayerInput();
        }

        protected override GameStateGameplay.GameplayData InitializeGameplayData()
        {
            GameplayData gpd = new GameplayDataMultiplayerLIVE();

            // intialize each player's gameplay data
            gpd.InitializePlayer(0);
            gpd.InitializePlayer(1);

            // network session has become invalid (e.g. host quit during the vs screen or we lost our connection)
            if (!NetworkSessionManager.Instance.IsValidSession) return gpd;

            // set a reference to the player that is hosting (if they are actually playing in this match that is)
            if (Game.Instance.ActiveGameplaySettings.Players[0].GamerTag == NetworkSessionManager.Instance.Session.Host.Gamertag)
            {
                (gpd as GameplayDataMultiplayerLIVE).HostPlayerIndex = 0;
            }
            else if (Game.Instance.ActiveGameplaySettings.Players[1].GamerTag == NetworkSessionManager.Instance.Session.Host.Gamertag)
            {
                (gpd as GameplayDataMultiplayerLIVE).HostPlayerIndex = 1;
            }
            else
            {
                (gpd as GameplayDataMultiplayerLIVE).HostPlayerIndex = -1;
            }

            // set a reference to the player that is the one local to this machine (if they are actually playing in this match that is)
            if (Game.Instance.ActiveGameplaySettings.Players[0].GamerTag == Game.Instance.Gamertag)
            {
                (gpd as GameplayDataMultiplayerLIVE).MyPlayerIndex = 0;
            }
            if (Game.Instance.ActiveGameplaySettings.Players[1].GamerTag == Game.Instance.Gamertag)
            {
                (gpd as GameplayDataMultiplayerLIVE).MyPlayerIndex = 1;
            }
            else
            {
                (gpd as GameplayDataMultiplayerLIVE).MyPlayerIndex = -1;
            }

            return gpd;
        }

        protected override void InitializeGamePlay()
        {
            if (_joiningState == EnumJoiningState.Joined)
            {
                Game.Instance.ActiveGameplaySettings.ResetScoreData();
                base.InitializeGamePlay();
            }

            _networkMessage.Clear();
        }

        protected override void GenerateQuestion()
        {
            // only generate question content if we are the host
            if (NetworkSessionManager.Instance.IsHosting())
            {
                _readyForQuestionContentCount.Clear();
                List<Player> players = (Game.Instance.ActiveGameplaySettings as GamePlaySettingsMultiplayerLIVE).AllPlayers;

                for (int i = 0; i < players.Count; i++)
                {
                    _readyForQuestionContentCount.Add(false);
                }

                base.GenerateQuestion();

                // send sync data so that new clients can sync up and will know to tell us they are ready for the next question
                Debug.WriteLine("Sending sync data to clients");
                NetworkMessage msg = new NetworkMessage(NetworkMessage.EnumType.SyncDataForNewClients, _health);
                LocalNetworkGamer sender = (NetworkSessionManager.Instance.Session.Host as LocalNetworkGamer);
                NetworkSessionManager.Instance.SendDataToAll(msg, sender);
            }
            // else let the host know that we are ready for the next question (we will pick up the actual question content when the host sends it to us)
            else
            {
                // tell the host we are ready (no need to wait for host to tell us they are ready as they will always be ready before us anyway - except when we are just joining, but that is handled in ProcessWaitingToWatch())
                Debug.WriteLine("Sending 'ready' message to host");
                NetworkMessage msg = new NetworkMessage(NetworkMessage.EnumType.ClientIsReadyForNextQuestion, null);
                LocalNetworkGamer sender = NetworkSessionManager.Instance.Session.LocalGamers[0];
                NetworkSessionManager.Instance.SendDataToHost(msg, sender);
            }
        }

        private void ProcessWaitingForWelcomePack()
        {
            // quit the game if the session is no longer valid (e.g. the host quit or we lost our connection, etc)
            // note: we must handle this here as we are not processing session events until we have joined properly
            // and we haven't joined properly yet.
            if (!NetworkSessionManager.Instance.IsValidSession)
            {
                if (_isExiting) return;

                _isExiting = true;
                _quitGameplay(null, null);

                return;
            }

            // only process welcome pack - anything else can be ignored (we won't get a player-win message until after the welcome pack because if we had joined that late we would not get our welcome until the host was back in the lobby anyway)
            // NOTE: we are garraunteed to get the welcome pack because the host sends it at the
            // same time as the message that told us to come to this gamestate
            if (_networkMessage.Type == NetworkMessage.EnumType.GamePlayWelcomePack)
            {
                // update the game based on the info in the welcome pack
                object[] info = _networkMessage.Data as object[];

                string[] allPlayers = info[0] as string[];
                string[] players = info[1] as string[];
                TutorManager.EnumTutor character1 = (TutorManager.EnumTutor)info[2];
                TutorManager.EnumTutor character2 = (TutorManager.EnumTutor)info[3];
                int health1 = (int)info[4];
                int health2 = (int)info[5];
                int questionNumber = (int)info[6];
                int energybar = (int)info[7];
                bool[] mathGrades = info[8] as bool[];
                TutorManager.EnumTutor location = (TutorManager.EnumTutor)info[9];
                TutorManager.EnumTutor locationToUse = (TutorManager.EnumTutor)info[10];

                // ...add players to player queue
                for (int i = 0; i < allPlayers.Length; i++)
                {
                    foreach (NetworkGamer gamer in NetworkSessionManager.Instance.Session.AllGamers)
                    {
                        if (gamer.Gamertag == allPlayers[i])
                        {
                            AddNetworkGamer(gamer);
                            break;
                        }
                    }
                }

                // ...update list of players actually playing in this match rather than just watching
                List<Player> allPlayerList = (Game.Instance.ActiveGameplaySettings as GamePlaySettingsMultiplayerLIVE).AllPlayers;

                for (int i = 0; i < players.Length; i++)
                {
                    foreach (Player player in allPlayerList)
                    {
                        if (player.GamerTag == players[i])
                        {
                            Game.Instance.ActiveGameplaySettings.Players[0] = player;
                            break;
                        }
                    }
                }

                // ...set the characters that are in use for this match
                Game.Instance.ActiveGameplaySettings.Players[0].Character = character1;
                Game.Instance.ActiveGameplaySettings.Players[1].Character = character2;

                // ...set some general gameplay settings
                Game.Instance.ActiveGameplaySettings.EnergyBar = energybar;
                Game.Instance.ActiveGameplaySettings.EnableMathGrades(mathGrades);
                Game.Instance.ActiveGameplaySettings.Location = location;
                Game.Instance.ActiveGameplaySettings.LocationToUse = locationToUse;

                // ...initialize the gamestate properly now - i.e. the stuff we didn't do when the gamestate first load because we didn't have the data yet
                UnloadJoiningScene();
                base.OnTransitionOnCompleted();
                //base.InitializeGamePlay();

                // ...update health and healthbars
                UpdateHealthFromNewData(0, health1);
                UpdateHealthFromNewData(1, health2);

                // ...update the question count
                _questionNumber = questionNumber;

                // now we need to wait to sync with the game properly so go wait for next question so we can properly start watching
                _joiningState = EnumJoiningState.WaitingToWatch;
            }

            _networkMessage.Consume();
        }

        private void AddNetworkGamer(NetworkGamer gamer)
        {
            // are you local?
            if (gamer.IsLocal)  // when a client is added to the session then they will see themselves as local
            {
                foreach (SignedInGamer localGamer in SignedInGamer.SignedInGamers)
                {
                    if (localGamer.Gamertag == gamer.Gamertag)
                    {
                        Game.Instance.ActiveGameplaySettings.AddPlayer(new PlayerLocal((int)(localGamer.PlayerIndex)));
                        break;
                    }
                }
            }
            // else must be from out of town
            else
            {
                Game.Instance.ActiveGameplaySettings.AddPlayer(new PlayerRemote(gamer));
            }
        }

        private void ProcessWaitingToWatch()
        {
            if (!NetworkSessionManager.Instance.IsValidSession) return;

            // only process sync data
            if (_networkMessage.Type == NetworkMessage.EnumType.SyncDataForNewClients)
            {
                Debug.WriteLine("Received sync data from host to sync us with the game properly");

                // update health and health bars
                int[] health = _networkMessage.Data as int[];
                UpdateHealthFromNewData(0, health[0]);
                UpdateHealthFromNewData(1, health[1]);

                // we've joined now
                _joiningState = EnumJoiningState.Joined;

                // tell the host we are ready
                Debug.WriteLine("New client sending 'ready' message to host");
                NetworkMessage msg = new NetworkMessage(NetworkMessage.EnumType.ClientIsReadyForNextQuestion, null);
                LocalNetworkGamer sender = NetworkSessionManager.Instance.Session.LocalGamers[0];
                NetworkSessionManager.Instance.SendDataToHost(msg, sender);

                // go wait for the question content
                _state = EnumGameplayState.WaitingForQuestionContent;
            }
            // or player-win
            else if (_networkMessage.Type == NetworkMessage.EnumType.PlayerXXXXWins)
            {
                int winner = (int)_networkMessage.Data;

                if (winner == 0)
                {
                    UpdateHealthFromNewData(1, _health[1]);
                    _health[1] = 0;
                }
                else
                {
                    UpdateHealthFromNewData(0, _health[0]);
                    _health[0] = 0;
                }

                // no need to do any more joining processing - the game is over anyhow
                _joiningState = EnumJoiningState.Joined;
                _state = EnumGameplayState.GameWon;
            }

            // anything else we aren't bothered about as we aren't synced to the game properly yet anyway
            _networkMessage.Consume();
        }

        private void UpdateHealthFromNewData(int player, int health)
        {
            int damage = health - _health[player];
            _health[player] = health;

            if (damage > 1)
            {
                AddAsyncTask(AsyncTask_UpdateHealthBarDisplay(player, damage), true);
            }
        }

        protected override void ProcessWaitingForQuestionContent()
        {
            // if we are the host then we need to wait for the client to tell us they are ready and then send them the
            // question content, before moving to the next gameplay state
            if (NetworkSessionManager.Instance.IsHosting())
            {
                if (_networkMessage.Type == NetworkMessage.EnumType.ClientIsReadyForNextQuestion)
                {
                    // find which player is saying they are ready
                    List<Player> players = (Game.Instance.ActiveGameplaySettings as GamePlaySettingsMultiplayerLIVE).AllPlayers;

                    for (int i = 0; i < players.Count; i++)
                    {
                        if (players[i].GamerTag == _networkMessage.Sender.Gamertag)
                        {
                            _readyForQuestionContentCount[i] = true;
                            Debug.WriteLine("Received 'ready' message from client: " + i);
                            break;
                        }
                    }

                    // check if all players are ready now
                    int readyCount = 0;

                    for (int i = 0; i < _readyForQuestionContentCount.Count; i++)
                    {
                        if (_readyForQuestionContentCount[i]) readyCount++;
                    }

                    // if they are then send out the question content
                    if (readyCount == _readyForQuestionContentCount.Count - 1)
                    {
                        Debug.WriteLine("Sending question content to clients");

                        NetworkMessage msg = new NetworkMessage(NetworkMessage.EnumType.QuestionContent, _gameplayData.Question);
                        LocalNetworkGamer sender = (NetworkSessionManager.Instance.Session.Host as LocalNetworkGamer);
                        NetworkSessionManager.Instance.SendDataToAll(msg, sender);

                        _state = EnumGameplayState.QuestionIntro;
                    }
                }

                _networkMessage.Consume();  // ignore irrelevant messages (it's possible the client sends some input data as the client might be lagging behind us a fraction)
            }
            // else wait for the host to send us the question content before moving on to the next game state
            else
            {
                if (_networkMessage.Type == NetworkMessage.EnumType.QuestionContent)
                {
                    Debug.WriteLine("Received question content from host");

                    _gameplayData.Question = _networkMessage.Data as QuestionContent;
                    _networkMessage.Consume();

                    _state = EnumGameplayState.QuestionIntro;
                }

                // ignore anything else
                _networkMessage.Consume();
            }

            // NOTE: unlike the base class we don't wait if the pause menu is showing - LIVE multiplayer games should not be paused
        }

        protected override void PlayWinLoseSFX()
        {
            GameplayDataMultiplayerLIVE data = (_gameplayData as GameplayDataMultiplayerLIVE);

            // watcher hears 'you win' regardless of who won
            if (data.MyPlayerIndex == -1)
            {
                MFSoundManager.Instance.PlaySFX(MFSoundManager.EnumSFX.YouWin);
            }
            // else play win/lose as appropriate
            else
            {
                int winner = _health[0] > 0 ? 0 : 1;

                if (data.MyPlayerIndex == winner)
                {
                    MFSoundManager.Instance.PlaySFX(MFSoundManager.EnumSFX.YouWin);
                }
                else
                {
                    MFSoundManager.Instance.PlaySFX(MFSoundManager.EnumSFX.YouLose);
                }
            }
        }

        protected override void OnGamerLeft(SessionEventInfo eventInfo)
        {
            if (!NetworkSessionManager.Instance.IsValidSession) return;

            base.OnGamerLeft(eventInfo);

            NetworkGamer gamer = (eventInfo.Args as GamerLeftEventArgs).Gamer;

            // update the locally stored player list
            List<Player> players = Game.Instance.ActiveGameplaySettings.Players;
            int count = players.Count;
            int removedIndx = -1;

            for (int i = 0; i < count; i++)
            {
                if (players[i].GamerTag == gamer.Gamertag)
                {
                    Game.Instance.ActiveGameplaySettings.RemovePlayer(players[i]);
                    removedIndx = i;
                    break;
                }
            }

            // if the player is one of the ones actively playing in the match then we should quit
            // to the lobby if it wasn't the host that quit (we will get an event to handle if it
            // was the host that quit and the resulting action to take will be quite different)
            if (!gamer.IsHost && (removedIndx == 0 || removedIndx == 1))
            {
                if (_isExiting) return; // we are ourselves already exiting

                // hide any menus or dialogs that are showing
                GameStateManager.Instance.PopOverlaysImmediately();

                // the player who is left should automatically win (unfortunately if the host quits we
                // don't get an auto win, of course, as host quitting has to be handled differently).
                // ...will goto the win gamestate automatically as soon as the auto win stuff has played out
                _isExiting = true;
                KillAllTasks(true, true);
                _state = EnumGameplayState.GameWon;
                AddAsyncTask(AsyncTask_DoAutoWin(OtherPlayer(removedIndx)), true);
            }
        }

        protected override void OnGamerJoined(SessionEventInfo eventInfo)
        {
            if (!NetworkSessionManager.Instance.IsValidSession) return;

            base.OnGamerJoined(eventInfo);
            
            NetworkGamer gamer = (eventInfo.Args as GamerJoinedEventArgs).Gamer;

            // check that the player is not already in the list
            if ((Game.Instance.ActiveGameplaySettings as GamePlaySettingsMultiplayerLIVE).PlayerIsQueued(gamer.Gamertag))
            {
                Debug.WriteLine("Gamer already joined: " + gamer.Gamertag);
                return;
            }

            // if we're hosting then send the new player info about where to find us and how to update their state when they get here
            if (NetworkSessionManager.Instance.IsValidSession && NetworkSessionManager.Instance.IsHosting())
            {
                // NOTE: don't worry about updating the ready status list as it doesn't need to grow when a gamer joins - it will be rebuilt on the next question anyway

                if (!gamer.IsHost)  // silliness check - shouldn't even get to this check if we're the host
                {
                    // let them know where we are
                    NetworkMessage msg = new NetworkMessage(NetworkMessage.EnumType.GameStateJoined, GameStateNames.GAMEPLAY_MULTIPLAYER_LIVE);
                    LocalNetworkGamer sender = (NetworkSessionManager.Instance.Session.Host as LocalNetworkGamer);
                    NetworkSessionManager.Instance.SendData(msg, sender, gamer);

                    // send the client a welcome message from this gamestate
                    List<Player> allPlayers = (Game.Instance.ActiveGameplaySettings as GamePlaySettingsMultiplayerLIVE).AllPlayers;
                    int length = allPlayers.Count;
                    string[] allGamerTags = new string[length];

                    for (int i = 0; i < length; i++)
                    {
                        allGamerTags[i] = allPlayers[i].GamerTag;
                    }

                    List<Player> players = Game.Instance.ActiveGameplaySettings.Players;
                    length = players.Count;
                    string[] playerGamerTags = new string[length];

                    for (int i = 0; i < length; i++)
                    {
                        playerGamerTags[i] = players[i].GamerTag;
                    }

                    msg = new NetworkMessage(NetworkMessage.EnumType.GamePlayWelcomePack,
                                                new object[11] {
                                                    allGamerTags,
                                                    playerGamerTags,
                                                    players[0].Character,
                                                    players[1].Character,
                                                    _health[0],
                                                    _health[1],
                                                    _questionNumber,
                                                    Game.Instance.ActiveGameplaySettings.EnergyBar,
                                                    Game.Instance.ActiveGameplaySettings.GetMathGrades(),
                                                    Game.Instance.ActiveGameplaySettings.Location,
                                                    Game.Instance.ActiveGameplaySettings.LocationToUse
                                                });

                    NetworkSessionManager.Instance.SendData(msg, sender, gamer);
                }
            }

            // update the locally stored player list
            AddNetworkGamer(gamer);
        }

        protected override void ProcessGameWon()
        {
            if (_isExiting) return;

            base.ProcessGameWon();

            _isExiting = true;

            // if we're hosting then tell clients (any late joiners will need to know who won if they aren't properly in sync with the game yet)
            if (NetworkSessionManager.Instance.IsValidSession && NetworkSessionManager.Instance.IsHosting())
            {
                // ...start by assuming player 1 won
                int winner = 0;

                // ...check if player 2 won?
                if (_health[0] == 0)
                {
                    winner = 1;
                }

                // ...send the message
                NetworkMessage msg = new NetworkMessage(NetworkMessage.EnumType.PlayerXXXXWins, winner);
                LocalNetworkGamer sender = (NetworkSessionManager.Instance.Session.Host as LocalNetworkGamer);
                NetworkSessionManager.Instance.SendDataToAll(msg, sender);
            }
        }

        protected override void OnSessionEnded(SessionEventInfo eventInfo)
        {
            //// if showing pause menu then don't display notifications yet - just push the event
            //// back on the queue and wait
            //if (_isShowingPauseMenu)
            //{
            //    NetworkSessionManager.Instance.PushBackEvent(eventInfo);
            //    return;
            //}

            base.OnSessionEnded(eventInfo);

            // add a waitlist item so that no other game processing will occur - the game is most definitely over now, but we will still be waiting for the player to press 'A' on the notification
            RegisterWaiting("SessionEnded");

            switch ((eventInfo.Args as NetworkSessionEndedEventArgs).EndReason)
            {
                case NetworkSessionEndReason.ClientSignedOut:
                    // we signed out (or were signed out - sometimes xbox live can sign us out of LIVE)
                    NetworkSessionManager.Instance.ShutdownSession();
                    GameStateManager.Instance.PopOverlaysImmediately();
                    DialogNotification.Show(DialogNotification.MESSAGE_PLAYERSIGNEDOUT, _quitGameplay);
                    break;

                case NetworkSessionEndReason.Disconnected:
                    // lost our connection to the game
                    NetworkSessionManager.Instance.ShutdownSession();
                    GameStateManager.Instance.PopOverlaysImmediately();
                    DialogNotification.Show(DialogNotification.MESSAGE_CONNECTIONLOST, _quitGameplay);
                    break;

                case NetworkSessionEndReason.HostEndedSession:
                    // host has ended the session
                    if (!NetworkSessionManager.Instance.IsHosting())
                    {
                        NetworkSessionManager.Instance.ShutdownSession();
                        GameStateManager.Instance.PopOverlaysImmediately();
                        DialogNotification.Show(DialogNotification.MESSAGE_HOSTENDEDMATCH, _quitGameplay);
                    }
                    break;

                case NetworkSessionEndReason.RemovedByHost:
                    // host has kicked us from the game (note: this can only be done from the lobby, but could potentially have happened just before the gameplay state was pushed)
                    NetworkSessionManager.Instance.ShutdownSession();
                    GameStateManager.Instance.PopOverlaysImmediately();
                    DialogNotification.Show(DialogNotification.MESSAGE_KICKEDFROMMATCH, _quitGameplay);
                    break;
            }
        }

        /// <summary>
        /// Will get the next available network message if the 'gameplay-global' network message
        /// property isn't already filled with a valid message.
        /// </summary>
        protected override void GetNextNetworkMessage()
        {
            if (_isExiting) return; // don't bother processing stuff if we are already exiting

            if (!NetworkSessionManager.Instance.IsValidSession) return;

            if (_networkMessage.Type == NetworkMessage.EnumType.Undefined)
            {
                if (NetworkSessionManager.Instance.IsHosting())
                {
                    LocalNetworkGamer localGamer = (NetworkSessionManager.Instance.Session.Host as LocalNetworkGamer);

                    if (localGamer.IsDataAvailable)
                    {
                        NetworkSessionManager.Instance.RecieveDataFromClient(_networkMessage, localGamer);
                    }
                }
                else
                {
                    LocalNetworkGamer localGamer = NetworkSessionManager.Instance.Session.LocalGamers[0];

                    if (localGamer.IsDataAvailable)
                    {
                        NetworkSessionManager.Instance.RecieveDataFromHost(_networkMessage, localGamer);
                    }
                }
            }
        }

        private static GUIActionDelegate _quitGameplay = new GUIActionDelegate(delegate(GUIComponent guiComponent, string paramString)
        {
            // WARNING: this check is now done before calling this delegate (so if anymore calls to this delegate are added be sure to do the check!)
            //if (_this._isExiting) return;

            //_this._isExiting = true;

            if (GameStateVsSplashSinglePlayer.HasSavedProgress)
            {
                GameStateManager.Instance.PopOverlaysImmediately();
                DialogNotification.Show(DialogNotification.MESSAGE_RETURNINGTOCHAMPIONSHIP, _returnToChampionship);
            }
            else
            {
                Debug.WriteLine("(Action) quitting gameplay");
                GameStateManager.Instance.ClearAndLoad(GameStateNames.MAINMENU, GameStateMainMenu.RETURNING_FROM_GAME);
            }
        });

        private static GUIActionDelegate _returnToChampionship = new GUIActionDelegate(delegate(GUIComponent guiComponent, string paramString)
        {
            Debug.WriteLine("(Action) quitting returning to championship");
            GameStateManager.Instance.ClearAndLoad(GameStateNames.VS_SPLASH_SINGLEPLAYER, null);
        });


        /// <summary>
        /// Extends the default gameplay data class to do data updating over xbox LIVE
        /// </summary>
        protected class GameplayDataMultiplayerLIVE : GameplayData
        {
            public int HostPlayerIndex;
            public int MyPlayerIndex;

            // client player input updating goes via the host so it takes a while for the actual update to happen.
            // so we remember what we sent to the host last and check whether current input differs from that before
            // we send another update - we can't compare with the current gameplaydata because that won't be
            // updated until the host gets back to us.
            private Player.EnumGamepadButton[] ModifiedPlayerInput = new Player.EnumGamepadButton[2];
            private bool[] ModifiedHintPressed = new bool[2];
            private bool[] ModifiedSuperAttackPressed = new bool[2];
            private bool[] ModifiedTauntPressed = new bool[2];


            public override void UpdatePlayerInput(float countdownRemaining)
            {
                if (!NetworkSessionManager.Instance.IsValidSession) return;

                CountDownRemaining_ToSend = countdownRemaining;

                if (NetworkSessionManager.Instance.IsHosting())
                {
                    UpdateHost();
                }
                else
                {
                    UpdateClient();
                }
            }

            private void UpdateHost()
            {
                bool sendPlayerInputToClient = false;

                // if the host is also one of the players in the match then update the player
                // input states if the host has pressed/released buttons.
                if (HostPlayerIndex != -1)
                {
                    Player hostPlayer = Game.Instance.ActiveGameplaySettings.Players[HostPlayerIndex];

                    if ((PlayerInput[HostPlayerIndex] == Player.EnumGamepadButton.None && hostPlayer.GetButtonPressed() != Player.EnumGamepadButton.None) ||
                        (HintPressed[HostPlayerIndex] != hostPlayer.HintPressed) ||
                        (TauntPressed[HostPlayerIndex] != hostPlayer.TauntPressed) ||
                        (SuperAttackPressed[HostPlayerIndex] != hostPlayer.SuperAttackPressed))
                    {
                        //      update local data using player's input
                        PlayerInput[HostPlayerIndex] = hostPlayer.GetButtonPressed();
                        HintPressed[HostPlayerIndex] = hostPlayer.HintPressed;
                        TauntPressed[HostPlayerIndex] = hostPlayer.TauntPressed;
                        SuperAttackPressed[HostPlayerIndex] = hostPlayer.SuperAttackPressed;

                        sendPlayerInputToClient = true;
                    }
                }

                // if there is network data get it and update using it
                if (_this._networkMessage.Type == NetworkMessage.EnumType.PlayerInputs)
                {
                    Debug.WriteLine("Receiving player input data from client");

                    object[] receivedPlayerInputData = _this._networkMessage.Data as object[];

                    // find which player sent the data and update our local player input for that player
                    List<Player> players = Game.Instance.ActiveGameplaySettings.Players;

                    for (int i = 0; i < 2; i++)
                    {
                        if (players[i].GamerTag == _this._networkMessage.Sender.Gamertag)
                        {
                            PlayerInput[i] = (receivedPlayerInputData[0] as Player.EnumGamepadButton[])[i];
                            HintPressed[i] = (receivedPlayerInputData[1] as bool[])[i];
                            TauntPressed[i] = (receivedPlayerInputData[2] as bool[])[i];
                            SuperAttackPressed[i] = (receivedPlayerInputData[3] as bool[])[i];
                            break;
                        }
                    }

                    // input gets sent out to clients - this is the input the sending client will actually process (i.e the host has to confirm it first so everything is in sync)
                    sendPlayerInputToClient = true;

                    _this._networkMessage.Consume();
                }

                // if any players (either local or remote) have pressed buttons then send the updated input data to clients
                if (sendPlayerInputToClient)
                {
                    Debug.WriteLine("Sending player input data to clients");

                    NetworkMessage msg = new NetworkMessage(NetworkMessage.EnumType.PlayerInputs, new object[5] { PlayerInput, HintPressed, TauntPressed, SuperAttackPressed, CountDownRemaining_ToSend });
                    LocalNetworkGamer sender = (NetworkSessionManager.Instance.Session.Host as LocalNetworkGamer);
                    NetworkSessionManager.Instance.SendDataToAll(msg, sender);

                    CountDownRemaining_ToUse = CountDownRemaining_ToSend;   // update our local countdown to use when deciding on multipliers (we only update this when there is actual input)
                }
            }

            private void UpdateClient()
            {
                // if we are playing in this match then if we pressed an answer button, or an extra
                // button (hint, superattack, taunt) then we will need to send that data to the host
                if (MyPlayerIndex != -1)
                {
                    Player clientplayer = Game.Instance.ActiveGameplaySettings.Players[MyPlayerIndex];

                    if ((ModifiedPlayerInput[MyPlayerIndex] == Player.EnumGamepadButton.None && clientplayer.GetButtonPressed() != Player.EnumGamepadButton.None) ||
                        (ModifiedHintPressed[MyPlayerIndex] != clientplayer.HintPressed) ||
                        (ModifiedTauntPressed[MyPlayerIndex] != clientplayer.TauntPressed) ||
                        (ModifiedSuperAttackPressed[MyPlayerIndex] != clientplayer.SuperAttackPressed))
                    {
                        Debug.WriteLine("Sending player input data to host");

                        ModifiedPlayerInput[MyPlayerIndex] = clientplayer.GetButtonPressed();
                        ModifiedHintPressed[MyPlayerIndex] = clientplayer.HintPressed;
                        ModifiedTauntPressed[MyPlayerIndex] = clientplayer.TauntPressed;
                        ModifiedSuperAttackPressed[MyPlayerIndex] = clientplayer.SuperAttackPressed;

                        NetworkMessage msg = new NetworkMessage(NetworkMessage.EnumType.PlayerInputs, new object[5] { ModifiedPlayerInput, ModifiedHintPressed, ModifiedTauntPressed, ModifiedSuperAttackPressed, CountDownRemaining_ToSend });
                        LocalNetworkGamer sender = NetworkSessionManager.Instance.Session.LocalGamers[0];   // this player will be the first (indeed only) local gamer in the session
                        NetworkSessionManager.Instance.SendDataToHost(msg, sender);
                    }
                }

                // if host sent an update for our gamedata then apply it (this is the playerinput that we will *actually* process in the game)
                if (_this._networkMessage.Type == NetworkMessage.EnumType.PlayerInputs)
                {
                    Debug.WriteLine("Receiving player input data from host");

                    object[] receivedPlayerInputData = _this._networkMessage.Data as object[];

                    // update our local input data for each player (the ones actually playing in the match)
                    PlayerInput[0] = (receivedPlayerInputData[0] as Player.EnumGamepadButton[])[0];
                    HintPressed[0] = (receivedPlayerInputData[1] as bool[])[0];
                    TauntPressed[0] = (receivedPlayerInputData[2] as bool[])[0];
                    SuperAttackPressed[0] = (receivedPlayerInputData[3] as bool[])[0];

                    PlayerInput[1] = (receivedPlayerInputData[0] as Player.EnumGamepadButton[])[1];
                    HintPressed[1] = (receivedPlayerInputData[1] as bool[])[1];
                    TauntPressed[1] = (receivedPlayerInputData[2] as bool[])[1];
                    SuperAttackPressed[1] = (receivedPlayerInputData[3] as bool[])[1];

                    CountDownRemaining_ToUse = (float)receivedPlayerInputData[4]; // synchronize our countdown timer with the host - for question answer multipliers at least

                    _this._networkMessage.Consume();
                }
            }

            public override void ResetPlayerInput()
            {
                base.ResetPlayerInput();

                // we also need to consume any lingering network messages about player input
                while (_this._networkMessage.Type == NetworkMessage.EnumType.PlayerInputs)
                {
                    _this._networkMessage.Consume();
                    _this.GetNextNetworkMessage();
                }
            }

            public override void ResetPlayerInput(int playerNum)
            {
                base.ResetPlayerInput(playerNum);

                ModifiedPlayerInput[playerNum] = Player.EnumGamepadButton.None;
                ModifiedSuperAttackPressed[playerNum] = false;
                ModifiedTauntPressed[playerNum] = false;
                ModifiedHintPressed[playerNum] = false;
            }
        }
    }
}
