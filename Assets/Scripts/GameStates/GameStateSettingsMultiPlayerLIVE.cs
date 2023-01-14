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
using MathFreak.GameStates.Dialogs;
using MathFreak.Highscores;



namespace MathFreak.GameStates
{
    /// <summary>
    /// This gamestate handles the game settings screen for LIVE multiplayer
    /// </summary>
    [TorqueXmlSchemaType]
    public class GameStateSettingsMultiPlayerLIVE : GameStateSettingsMultiPlayer
    {
        public const string ISJOINING = "isjoining";
        public const string WAS_CHALLENGED = "waschallenged";

        private NetworkMessage _networkMessage = new NetworkMessage();

        private RenderMaterial _chatIconHeadset;
        private RenderMaterial _chatIconHeadsetTalking;

        private bool[] _customGradeSettingsFromHost;


        public override void OnTransitionOnCompleted()
        {
            _showChatIcon = true;

            if (NetworkSessionManager.Instance.IsValidSession && NetworkSessionManager.Instance.IsHosting())
            {
                _showKickPrompt = true;
            }

            base.OnTransitionOnCompleted();

            //// if we were in a single player match and got challenged then we need
            //// to tell the challenger that we are in the lobby now and they should join.
            //if (ParamString == WAS_CHALLENGED && NetworkSessionManager.Instance.IsValidSession)
            //{
            //    // could be more than one person simultaneously joining us - so just add them all
            //    foreach (NetworkGamer challenger in NetworkSessionManager.Instance.Session.RemoteGamers)
            //    {
            //        AddNewGamer(challenger);
            //    }
            //}

            // send out our score data so the other players will be able to update their leaderboards and also see our gradebadge
            if (NetworkSessionManager.Instance.IsValidSession)
            {
                HighscoreData.PlayerScoreData scoreData = new HighscoreData.PlayerScoreData();
                scoreData.MultiplayerScores = new HighscoreData.MultiplayerScoreData[1];

                HighscoreData.SinglePlayerScoreData spData = HighscoreData.Instance.GetSinglePlayerHighscore(Game.Instance.Gamertag);
                HighscoreData.MultiplayerScoreData mpData = HighscoreData.Instance.GetMultiplayerHighscore(Game.Instance.Gamertag);

                if (spData != null)
                {
                    scoreData.SinglePlayerScores = new HighscoreData.SinglePlayerScoreData[1];
                    scoreData.SinglePlayerScores[0] = HighscoreData.Instance.GetSinglePlayerHighscore(Game.Instance.Gamertag);
                    scoreData.SpCount = 1;
                }


                if (mpData != null)
                {
                    scoreData.MultiplayerScores = new HighscoreData.MultiplayerScoreData[1];
                    scoreData.MultiplayerScores[0] = HighscoreData.Instance.GetMultiplayerHighscore(Game.Instance.Gamertag);
                    scoreData.MpCount = 1;
                }

                NetworkMessage msg = new NetworkMessage(NetworkMessage.EnumType.Highscores, scoreData);

                LocalNetworkGamer sender;

                if (NetworkSessionManager.Instance.Session.IsHost)
                {
                    sender = NetworkSessionManager.Instance.Session.Host as LocalNetworkGamer;
                }
                else
                {
                    sender = NetworkSessionManager.Instance.Session.LocalGamers[0];
                }

                NetworkSessionManager.Instance.SendDataToAll(msg, sender);
            }

            // host needs to update the session properties as they don't seem to update properly on initial session creation
            // we also need to make sure the session is not in the 'playing' state, so that other players can join
            if (NetworkSessionManager.Instance.IsValidSession && NetworkSessionManager.Instance.IsHosting())
            {
                NetworkSessionManager.Instance.Session.SessionProperties[(int)NetworkSessionManager.EnumSessionProperty.Grades] = Game.Instance.ActiveGameplaySettings.GetGradesAsInt();

                if (NetworkSessionManager.Instance.Session.SessionState == NetworkSessionState.Playing)
                {
                    NetworkSessionManager.Instance.Session.EndGame();
                }
            }
        }

        protected override void InitializeScene()
        {
            // grab references to the chat icon materials
            _chatIconHeadset = TorqueObjectDatabase.Instance.FindObject<RenderMaterial>("Chat_icon_Headset_TalkMaterial");
            _chatIconHeadsetTalking = TorqueObjectDatabase.Instance.FindObject<RenderMaterial>("Chat_HeadsetMaterial");

            // continue with scene initialization
            base.InitializeScene();
            TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("settings_kickplayers").Visible = true;
        }

        protected override GUIComponent GetDefaultFocusedGUIComponent()
        {
            if (NetworkSessionManager.Instance.IsValidSession && NetworkSessionManager.Instance.IsHosting())
            {
                return base.GetDefaultFocusedGUIComponent();
            }
            else
            {
                return null;
            }
        }

        protected override int MinPlayersRequired()
        {
            return 2;
        }

        protected override List<Player> GetPlayerList()
        {
            return (Game.Instance.ActiveGameplaySettings as GamePlaySettingsMultiplayerLIVE).AllPlayers;
        }

        protected void GetNextNetworkMessage()
        {
            //Debug.WriteLine("getting message?????");

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
                    //Debug.WriteLine("checking for data.....");
                    LocalNetworkGamer localGamer = NetworkSessionManager.Instance.Session.LocalGamers[0];

                    if (localGamer.IsDataAvailable)
                    {
                        //Debug.WriteLine("There is data available!!!!!");
                        NetworkSessionManager.Instance.RecieveDataFromAny(_networkMessage, localGamer);

                        // ignore messages from ourselves
                        if (_networkMessage.Sender.Gamertag == Game.Instance.Gamertag)
                        {
                            _networkMessage.Consume();
                        }
                    }
                }
            }
        }

        protected override void ProcessMessages()
        {
            if (!NetworkSessionManager.Instance.IsValidSession) return;

            if (_isExiting) return;

            GetNextNetworkMessage();

            switch (_networkMessage.Type)
            {
                case NetworkMessage.EnumType.Highscores:
                    UpdateScoreData();
                    break;

                case NetworkMessage.EnumType.SettingsChanged:
                    UpdateSettingsFromHost();
                    break;

                case NetworkMessage.EnumType.PlayerList:
                    UpdatePlayerListFromHost();
                    break;

                case NetworkMessage.EnumType.GotoSelectionScreen:
                    if (!_isExiting)
                    {
                        _isExiting = true;
                        Game.Instance.ActiveGameplaySettings.LocationToUse = Util.StringToEnum<TutorManager.EnumTutor>(_networkMessage.Data as string);
                        GotoSelectionScreen();
                    }
                    break;

                case NetworkMessage.EnumType.Undefined:
                    break;

                default:
                    Assert.Warn(false, "settings-lobby: Network message not valid in this context: " + _networkMessage.Type);
                    break;
            }

            _networkMessage.Consume();
        }

        private void UpdateScoreData()
        {
            Debug.WriteLine("Updating leaderboards using score data sent by another player in the settings lobby");

            HighscoreData.PlayerScoreData scoreData = _networkMessage.Data as HighscoreData.PlayerScoreData;
            HighscoreData.Instance.IntegrateNewSinglePlayerScores(scoreData.SinglePlayerScores, 0, scoreData.SinglePlayerScores.Length);
            HighscoreData.Instance.IntegrateNewMultiplayerScores(scoreData.MultiplayerScores, 0, scoreData.MultiplayerScores.Length);

            // will force the grade badge displays to update
            UpdatePlayers();
        }

        private void UpdateSettingsFromHost()
        {
            Debug.WriteLine("UpdateSettingsFromHost");

            GamePlaySettings settings = _networkMessage.Data as GamePlaySettings;

            Game.Instance.ActiveGameplaySettings.EnergyBar = settings.EnergyBar;
            Game.Instance.ActiveGameplaySettings.MathGradesSetting = settings.MathGradesSetting;
            Game.Instance.ActiveGameplaySettings.EnableMathGrades(settings);
            Game.Instance.ActiveGameplaySettings.Location = settings.Location;

            _customGradeSettingsFromHost = settings.GetMathGrades();

            OnUpdatedMathGradeSettings();
            UpdateSettingsDisplays();
        }

        protected override bool[] GetCustomSettings()
        {
            if (NetworkSessionManager.Instance.IsValidSession && NetworkSessionManager.Instance.Session.IsHost)
            {
                return Game.Instance.CustomMathGradeSelections[0];
            }
            else
            {
                if (_customGradeSettingsFromHost != null)
                {
                    return _customGradeSettingsFromHost;
                }
                else
                {
                    return new bool[14] { false, false, false, false, false, false, false, false, false, false, false, false, false, false };
                }
            }
        }

        protected void UpdatePlayerListFromHost()
        {
            Debug.WriteLine("UpdatePlayerListFromHost");

            string[] gamerTags = _networkMessage.Data as string[];

            Game.Instance.ActiveGameplaySettings.RemoveAllPlayers();

            for (int i = 0; i < gamerTags.Length; i++)
            {
                foreach (NetworkGamer gamer in NetworkSessionManager.Instance.Session.AllGamers)
                {
                    if (gamer.Gamertag == gamerTags[i])
                    {
                        AddNetworkGamer(gamer);
                        break;
                    }
                }
            }

            UpdatePlayers();
        }

        protected override void OnSettingsChanged()
        {
            if (!NetworkSessionManager.Instance.IsValidSession) return;

            // if we are the host then let clients know that we changed the settings
            if (NetworkSessionManager.Instance.IsHosting())
            {
                BroadcastSettings();
            }
        }

        private void BroadcastSettings()
        {
            // broadcast settings to clients already in the game
            NetworkMessage msg = new NetworkMessage(NetworkMessage.EnumType.SettingsChanged, Game.Instance.ActiveGameplaySettings);
            LocalNetworkGamer sender = (NetworkSessionManager.Instance.Session.Host as LocalNetworkGamer);
            NetworkSessionManager.Instance.SendDataToAll(msg, sender);

            // update the session properties so people deciding to join can see what the settings are
            NetworkSessionManager.Instance.Session.SessionProperties[(int)NetworkSessionManager.EnumSessionProperty.Grades] = Game.Instance.ActiveGameplaySettings.GetGradesAsInt();
        }

        protected override void UpdateReadyStatuses()
        {
            // the player list will not necessarily be in the same order as the network session's
            // gamer list so we need to map the ready statuses onto the player list one at a time.
            GamerCollection<NetworkGamer> gamerList = NetworkSessionManager.Instance.Session.AllGamers;
            List<Player> players = (Game.Instance.ActiveGameplaySettings as GamePlaySettingsMultiplayerLIVE).AllPlayers;

            for (int i = 0; i < players.Count; i++)
            {
                for (int j = 0; j < gamerList.Count; j++)
                {
                    if (players[i].GamerTag == gamerList[j].Gamertag)
                    {
                        _readyStatus[i] = gamerList[j].IsReady;
                    }
                }
            }
        }

        protected override void UpdateChatIcon(T2DStaticSprite icon, string gamertag)
        {
            icon.Visible = false;

            if (!NetworkSessionManager.Instance.IsValidSession) return;

            // find the gamer in the network session
            GamerCollection<NetworkGamer> gamerList = NetworkSessionManager.Instance.Session.AllGamers;

            for (int i = 0; i < gamerList.Count; i++)
            {
                if (gamerList[i].Gamertag == gamertag)
                {
                    if (gamerList[i].HasVoice)
                    {
                        if (gamerList[i].IsTalking)
                        {
                            icon.Visible = true;
                            icon.Material = _chatIconHeadset;
                        }
                        else
                        {
                            icon.Visible = true;
                            icon.Material = _chatIconHeadsetTalking;
                        }
                    }

                    break;
                }
            }
        }

        protected override void OnGamerLeft(SessionEventInfo eventInfo)
        {
            if (!NetworkSessionManager.Instance.IsValidSession) return;

            // if we are hosting then update the list (will also broadcast to clients)
            if (NetworkSessionManager.Instance.IsHosting())
            {
                Debug.WriteLine("Gamer left: " + (eventInfo.Args as GamerLeftEventArgs).Gamer.Gamertag);

                // remove the player from the list
                (Game.Instance.ActiveGameplaySettings as GamePlaySettingsMultiplayerLIVE).RemovePlayerByGamerTag((eventInfo.Args as GamerLeftEventArgs).Gamer.Gamertag);

                // and then broadcast it to clients
                BroadcastPlayerList();

                // do the usual updating
                base.OnGamerLeft(eventInfo);
            }
        }

        protected override void OnGamerJoined(SessionEventInfo eventInfo)
        {
            if (!NetworkSessionManager.Instance.IsValidSession) return;

            // if we are hosting then update the list (will also broadcast to clients)
            if (NetworkSessionManager.Instance.IsHosting())
            {
                // adding the new gamer will update the player list and broadcast stuff as necessary
                AddNewGamer((eventInfo.Args as GamerJoinedEventArgs).Gamer);

                // do the usual updating
                base.OnGamerJoined(eventInfo);
            }
        }

        private void AddNewGamer(NetworkGamer gamer)
        {
            // check that the player is not already in the list
            if ((Game.Instance.ActiveGameplaySettings as GamePlaySettingsMultiplayerLIVE).PlayerIsQueued(gamer.Gamertag))
            {
                Debug.WriteLine("Gamer already added: " + gamer.Gamertag);
                return;
            }

            Debug.WriteLine("Adding gamer: " + gamer.Gamertag);

            // if the player is not the host then send them a message to tell them where to find us
            NetworkMessage msg = new NetworkMessage(NetworkMessage.EnumType.GameStateJoined, GameStateNames.SETTINGS_MULTIPLAYER_LIVE);
            LocalNetworkGamer sender = (NetworkSessionManager.Instance.Session.Host as LocalNetworkGamer);
            NetworkSessionManager.Instance.SendData(msg, sender, gamer);

            // add the new player to the list
            AddNetworkGamer(gamer);

            // and broadcast the list to clients
            BroadcastPlayerList();

            // a new player was added so they will need to know what the settings currently are
            BroadcastSettings();
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

        private void BroadcastPlayerList()
        {
            // send player list to clients
            List<Player> players = (Game.Instance.ActiveGameplaySettings as GamePlaySettingsMultiplayerLIVE).AllPlayers;
            int length = players.Count;
            string[] gamerTags = new string[length];

            for (int i = 0; i < length; i++)
            {
                gamerTags[i] = players[i].GamerTag;
            }

            NetworkMessage msg = new NetworkMessage(NetworkMessage.EnumType.PlayerList, gamerTags);
            LocalNetworkGamer sender = (NetworkSessionManager.Instance.Session.Host as LocalNetworkGamer);
            NetworkSessionManager.Instance.SendDataToAll(msg, sender);
        }

        protected override void OnSessionEnded(SessionEventInfo eventInfo)
        {
            //// if showing the quit dialog then don't display notifications yet - just push the event
            //// back on the queue and wait (actually this would never happen anyway, but silliness
            //// check it while we're here)
            //if (IsWaiting("quitsettingslobby"))
            //{
            //    NetworkSessionManager.Instance.PushBackEvent(eventInfo);
            //    return;
            //}

            if (!_isExiting)
            {
                _isExiting = true;  // the session is ended, so we *will* be exiting the lobby
                                    // NOTE: this is a classic case of an 'exiting' process that
                                    // takes time - we have to wait for the user to press okay.

                // show an appropriate notification message (or none, as the case may be)
                switch ((eventInfo.Args as NetworkSessionEndedEventArgs).EndReason)
                {
                    case NetworkSessionEndReason.ClientSignedOut:
                        // just return to the mainmenu - no popup required (the player signed out so they don't need to be told by us that they signed out!)
                        NetworkSessionManager.Instance.ShutdownSession();
                        HideDialogsImmediately();
                        GameStateManager.Instance.ClearAndLoad(GameStateNames.MAINMENU, null);
                        break;

                    case NetworkSessionEndReason.Disconnected:
                        // lost our connection to the game
                        NetworkSessionManager.Instance.ShutdownSession();
                        HideDialogsImmediately();
                        DialogNotification.Show(DialogNotification.MESSAGE_CONNECTIONLOST, _quitLobby);
                        break;

                    case NetworkSessionEndReason.HostEndedSession:
                        // host has ended the session
                        if (!NetworkSessionManager.Instance.IsHosting())
                        {
                            NetworkSessionManager.Instance.ShutdownSession();
                            HideDialogsImmediately();
                            DialogNotification.Show(DialogNotification.MESSAGE_HOSTENDEDMATCH, _quitLobby);
                        }
                        break;

                    case NetworkSessionEndReason.RemovedByHost:
                        // host has kicked us from the game
                        Assert.Fatal(!NetworkSessionManager.Instance.IsHosting(), "Silliness - host has kicked the host from the game!");
                        NetworkSessionManager.Instance.ShutdownSession();
                        HideDialogsImmediately();
                        DialogNotification.Show(DialogNotification.MESSAGE_KICKEDFROMMATCH, _quitLobby);
                        break;
                }
            }
        }

        private void HideDialogsImmediately()
        {
            GameStateManager.Instance.PopOverlaysImmediately();
        }

        protected override bool OnAction_Continue()
        {
            if (_isExiting) return false;

            // if we're hosting then goto the selection screen and tell clients to do the same
            if (NetworkSessionManager.Instance.IsValidSession && NetworkSessionManager.Instance.IsHosting())
            {
                if (base.OnAction_Continue())
                {
                    // tell the clients to go to the character selection screen now - and also what location was picked (because could have been a random one)
                    NetworkMessage msg = new NetworkMessage(NetworkMessage.EnumType.GotoSelectionScreen, Game.Instance.ActiveGameplaySettings.LocationToUse.ToString());
                    LocalNetworkGamer sender = (NetworkSessionManager.Instance.Session.Host as LocalNetworkGamer);
                    NetworkSessionManager.Instance.SendDataToAll(msg, sender);
                    return true;
                }
            }

            return false;
        }

        protected override void GotoSelectionScreen()
        {
            base.GotoSelectionScreen();

            // store settings as 'most recently used' (these will be saved to disk when player returns to dashboard so that last settings are remembered)
            Game.Instance.LastLIVESettings = new GamePlaySettingsMultiplayerLIVE(Game.Instance.ActiveGameplaySettings as GamePlaySettingsMultiplayerLIVE);
            Game.Instance.LastLIVESettings.RemoveAllPlayers();
            Game.Instance.LastLIVESettings.ResetScoreData();

//#if XBOX
            Debug.WriteLine("(Action) pushing gamestate character selection");

            // setup the players who will actually play the match
            (Game.Instance.ActiveGameplaySettings as GamePlaySettingsMultiplayerLIVE).UpdateMatchPlayers();

            // if we are hosting then clear all the player 'ready' statuses so they are not 'auto-ready' when we return to the lobby
            // also 'start' the game so no one else can join (e.g. if the other player quits then other people could join mid-game if we don't prevent by 'starting' the game officially)
            if (NetworkSessionManager.Instance.IsValidSession && NetworkSessionManager.Instance.IsHosting())
            {
                NetworkSessionManager.Instance.Session.ResetReady();
                NetworkSessionManager.Instance.Session.StartGame();
            }

            GameStateManager.Instance.ClearAndLoad(GameStateNames.CHARACTERSELECTION_MULTIPLAYERLIVE, null);
//#else
//                // PC version can't do multiplayer stuff
//                Debug.WriteLine("(Action) (PC version) *not* pushing gamestate Lobby character selection");
//#endif
        }

        protected override void ShowCustomQuitDialog()
        {
            if (GameStateVsSplashSinglePlayer.HasSavedProgress)
            {
                GameStateManager.Instance.PushOverlay(GameStateNames.DIALOG_QUITBACKTOCHAMPIONSHIP, null);
            }
            else
            {
                base.ShowCustomQuitDialog();
            }
        }

        private void OnAction_KickPlayers()
        {
            if (NetworkSessionManager.Instance.IsValidSession && NetworkSessionManager.Instance.IsHosting())
            {
                AddAsyncTask(AsyncTask_ShowKickPlayersDialog(), true);
            }
        }

        protected IEnumerator<AsyncTaskStatus> AsyncTask_ShowKickPlayersDialog()
        {
            if (_isExiting) yield break;

            // if no other players then don't show the dialog as there's no one to kick
            if (NetworkSessionManager.Instance.Session.RemoteGamers.Count == 0) yield break;

            // else show the dialog
            string kickableGamerTag = NetworkSessionManager.Instance.Session.RemoteGamers[0].Gamertag;
            DialogKickPlayers.ResetResponse();
            DialogKickPlayers.Show(kickableGamerTag);

            // wait for a response from the user
            while (!DialogKickPlayers.ResponseRecieved) yield return null;

            yield return null;  // allow an extra tick so the dialog is *definitely* finished unloading itself before we unload ourselves too - or things could get messed up.

            if (_isExiting) yield break;

            // process the response
            switch (DialogKickPlayers.Response)
            {
                case DialogKickPlayers.EnumResponse.Cancelled:
                case DialogKickPlayers.EnumResponse.None: // will be none if the user hit escape/B-button to close the dialog
                    // nothing to do - just carry on as we were
                    break;

                case DialogKickPlayers.EnumResponse.KickPlayer1:
                    KickPlayer(kickableGamerTag);
                    break;
            }
        }

        private void KickPlayer(string gamertag)
        {
            if (!NetworkSessionManager.Instance.IsValidSession) return;

            // find the named remote player and kick them
            foreach (NetworkGamer gamer in NetworkSessionManager.Instance.Session.RemoteGamers)
            {
                if (gamer.Gamertag == gamertag)
                {
                    gamer.Machine.RemoveFromSession();
                    break;
                }
            }
        }


        public static GUIActionDelegate KickPlayers { get { return _kickPlayers; } }

        private static GUIActionDelegate _kickPlayers = new GUIActionDelegate(delegate(GUIComponent guiComponent, string paramString)
        {
            if (_activeInstance is GameStateSettingsMultiPlayerLIVE)
            {
                (_activeInstance as GameStateSettingsMultiPlayerLIVE).OnAction_KickPlayers();
            }
        });

        
        ////////////////////////////////////////////////////////////////////
        // private action stuff - not for selecting in the editor
        ////////////////////////////////////////////////////////////////////

        // called when we are displaying a session status dialog, such as 'connection lost' and we need
        // to immediately quit the lobby when the player clicks 'continue' on that dialog.
        private static GUIActionDelegate _quitLobby = new GUIActionDelegate(delegate(GUIComponent guiComponent, string paramString)
        {
            // NOTE: _isExiting is already checked and set by the calling code

            Debug.WriteLine("(Action) quitting the lobby");
            (_activeInstance as GameStateSettingsMultiPlayerLIVE).OnQuitting();
            GameStateManager.Instance.ClearAndLoad(GameStateNames.MAINMENU, null);
        });
    }
}
