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
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.GamerServices;
using MathFreak.GameStates.Dialogs;
using MathFreak.Text;



namespace MathFreak.GameStates
{
    /// <summary>
    /// The LIVE multiplayer specific version of the character selection screen.
    /// </summary>
    [TorqueXmlSchemaType]
    public class GameStateCharacterSelectionLobbyMultiplayerLIVE : GameStateCharacterSelectionLobby
    {
        private NetworkMessage _networkMessage = new NetworkMessage();
        private bool IsJoining;

        private const float CHARACTER_SELECTION_TIMEOUT = 30.0f;
        private float _elapsedTime;


        public override void PreTransitionOn(string paramString)
        {
            base.PreTransitionOn(paramString);

            IsJoining = (ParamString == GameStateSettingsMultiPlayerLIVE.ISJOINING);
            _elapsedTime = 0.0f;
        }

        protected void GetNextNetworkMessage()
        {
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
                        NetworkSessionManager.Instance.RecieveDataFromAny(_networkMessage, localGamer);

                        // if the data is not from the host and it is not a 'selection movement' or it is
                        // data we sent ourselves then ignore this message.
                        if (!_networkMessage.Sender.IsHost &&
                            (_networkMessage.Type != NetworkMessage.EnumType.SelectionMovement || _networkMessage.Sender.Gamertag == Game.Instance.Gamertag))
                        {
                            _networkMessage.Consume();
                        }
                    }
                }
            }
        }

        public override void ProcessTick(float dt)
        {
            if (!IsJoining)
            {
                base.ProcessTick(dt);
            }

            ProcessMessages();

            if (NetworkSessionManager.Instance.IsValidSession)
            {
                // we enforce a timeout on character selection so the game will continue even if someone
                // goes off to get a coffee or something - the other players can carry on playing because
                // a character will get chosen automatically for the inactive player.
                _elapsedTime += dt;

                if (_elapsedTime > CHARACTER_SELECTION_TIMEOUT)
                {
                    TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("selection_counter").Components.FindComponent<MathMultiTextComponent>().TextValue = "0";

                    if (NetworkSessionManager.Instance.IsHosting())
                    {
                        if (_player1_selectedCharacter == TutorManager.EnumTutor.None)
                        {
                            OnAction_Pressed_A(PLAYER_1);
                        }

                        if (_player2_selectedCharacter == TutorManager.EnumTutor.None)
                        {
                            OnAction_Pressed_A(PLAYER_2);
                        }
                    }
                }
                else
                {
                    TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("selection_counter").Components.FindComponent<MathMultiTextComponent>().TextValue = ((int)(CHARACTER_SELECTION_TIMEOUT - _elapsedTime)).ToString();
                }
            }
        }

        private void ProcessMessages()
        {
            if (!NetworkSessionManager.Instance.IsActiveSession) return;

            if (_isExiting) return;

            // only process events if we have done any joining stuff already - else we need to wait to process the welcome pack before we process any events
            // ...exception is if session ended event has been recieved as then any other message and event stuff is moot
            if (!IsJoining || !NetworkSessionManager.Instance.IsValidSession)
            {
                // process network events
                while (NetworkSessionManager.Instance.EventCount > 0)
                {
                    if (_isExiting) break;  // if already exiting the gamestate then no point processing any more messages

                    SessionEventInfo eventInfo = NetworkSessionManager.Instance.GetNextEvent();

                    if (eventInfo != null)
                    {
                        // process the event
                        switch (eventInfo.Type)
                        {
                            case SessionEventInfo.EnumSessionEvent.GamerLeft:
                                OnGamerLeft(eventInfo);
                                break;

                            case SessionEventInfo.EnumSessionEvent.GamerJoined:
                                OnGamerJoined(eventInfo);
                                break;

                            case SessionEventInfo.EnumSessionEvent.SessionEnded:
                                OnSessionEnded(eventInfo);
                                break;
                        }
                    }
                }
            }

            if (_isExiting) return;

            GetNextNetworkMessage();

            // if not in the process of joining then process messages as usual
            if (!IsJoining)
            {
                // process network messages
                switch (_networkMessage.Type)
                {
                    case NetworkMessage.EnumType.SelectionMovement:
                        UpdateMovementFromNetworkData();
                        break;

                    case NetworkMessage.EnumType.SelectionsChanged:
                        UpdateSelectionsFromNetworkData();
                        break;

                    case NetworkMessage.EnumType.GotoVsScreen:
                        Debug.WriteLine("msg from host: GotoVsScreen");
                        AddAsyncTask(AsyncTask_GotoVsScreen(), true);
                        break;

                    case NetworkMessage.EnumType.Undefined:
                        break;

                    case NetworkMessage.EnumType.GameStateJoined:
                        break;

                    case NetworkMessage.EnumType.SelectionWelcomePack:
                        break;

                    default:
                        Assert.Warn(false, "selection screen: Network message not valid in this context: " + _networkMessage.Type);
                        break;
                }
            }
            // else wait until we get the welcome pack
            else
            {
                // process the welcome pack - ignore any other messages
                if (_networkMessage.Type == NetworkMessage.EnumType.SelectionWelcomePack)
                {
                    UpdateFromWelcomePack();
                }
            }

            _networkMessage.Consume();
        }

        protected void OnGamerJoined(SessionEventInfo eventInfo)
        {
            if (!NetworkSessionManager.Instance.IsValidSession) return;

            NetworkGamer gamer = (eventInfo.Args as GamerJoinedEventArgs).Gamer;

            // check that the player is not already in the list
            if ((Game.Instance.ActiveGameplaySettings as GamePlaySettingsMultiplayerLIVE).PlayerIsQueued(gamer.Gamertag))
            {
                Debug.WriteLine("Gamer already joined: " + gamer.Gamertag);
                return;
            }

            // if we are hosting and the 'joiner' is not us then the client that joined needs some info sent to them
            if (NetworkSessionManager.Instance.IsHosting() && !gamer.IsHost)
            {
                Debug.WriteLine("Gamer joined: " + gamer.Gamertag);

                // send a message to the client to tell them which gamestate we are in
                NetworkMessage msg = new NetworkMessage(NetworkMessage.EnumType.GameStateJoined, GameStateNames.CHARACTERSELECTION_MULTIPLAYERLIVE);
                LocalNetworkGamer sender = (NetworkSessionManager.Instance.Session.Host as LocalNetworkGamer);
                NetworkSessionManager.Instance.SendData(msg, sender, gamer);

                // send the client a welcome message from this gamestate (with enough info for them to get in sync)
                List<Player> players = (Game.Instance.ActiveGameplaySettings as GamePlaySettingsMultiplayerLIVE).AllPlayers;
                int length = players.Count;
                string[] gamerTags = new string[length];

                for (int i = 0; i < length; i++)
                {
                    gamerTags[i] = players[i].GamerTag;
                }

                msg = new NetworkMessage(NetworkMessage.EnumType.SelectionWelcomePack,
                                            new object[5] {
                                                gamerTags,
                                                _player1_hilightedCharacter,
                                                _player2_hilightedCharacter,
                                                _player1_selectedCharacter,
                                                _player2_selectedCharacter
                                            });

                NetworkSessionManager.Instance.SendData(msg, sender, gamer);
            }

            // update the locally stored player list
            Game.Instance.ActiveGameplaySettings.AddPlayer(new PlayerRemote(gamer));
        }

        protected void OnGamerLeft(SessionEventInfo eventInfo)
        {
            if (!NetworkSessionManager.Instance.IsValidSession) return;

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
                //// if showing the quit dialog then don't display notifications yet - just push the event
                //// back on the queue and wait (actually this would never happen anyway, but silliness
                //// check it while we're here)
                //if (IsWaiting("quitselectionscreen"))
                //{
                //    NetworkSessionManager.Instance.PushBackEvent(eventInfo);
                //    return;
                //}

                if (_isExiting) return; // we are already exiting anyway - silliness check as messages shouldn't even be getting processed if we are exiting

                _isExiting = true;
                GameStateManager.Instance.PopOverlaysImmediately();
                DialogNotification.Show(DialogNotification.MESSAGE_PLAYERLEFT, _returnToLobby);
            }
        }

        protected void OnSessionEnded(SessionEventInfo eventInfo)
        {
            //// if showing the quit dialog then don't display notifications yet - just push the event
            //// back on the queue and wait (actually this would never happen anyway, but silliness
            //// check it while we're here)
            //if (IsWaiting("quitselectionscreen"))
            //{
            //    NetworkSessionManager.Instance.PushBackEvent(eventInfo);
            //    return;
            //}

            if (!_isExiting)
            {
                _isExiting = true;  // the session is ended, so we *will* be exiting the character selection screen

                // show an appropriate notification message (or none, as the case may be)
                switch ((eventInfo.Args as NetworkSessionEndedEventArgs).EndReason)
                {
                    case NetworkSessionEndReason.ClientSignedOut:
                        // just return to the mainmenu - no popup required (the player signed out so they don't need to be told by us that they signed out!)
                        NetworkSessionManager.Instance.ShutdownSession();
                        GameStateManager.Instance.PopOverlaysImmediately();
                        GameStateManager.Instance.ClearAndLoad(GameStateNames.MAINMENU, null);
                        break;

                    case NetworkSessionEndReason.Disconnected:
                        // lost our connection to the game
                        NetworkSessionManager.Instance.ShutdownSession();
                        GameStateManager.Instance.PopOverlaysImmediately();
                        DialogNotification.Show(DialogNotification.MESSAGE_CONNECTIONLOST, _quitSelectionScreen);
                        break;

                    case NetworkSessionEndReason.HostEndedSession:
                        // host has ended the session
                        if (!NetworkSessionManager.Instance.IsHosting())
                        {
                            NetworkSessionManager.Instance.ShutdownSession();
                            GameStateManager.Instance.PopOverlaysImmediately();
                            DialogNotification.Show(DialogNotification.MESSAGE_HOSTENDEDMATCH, _quitSelectionScreen);
                        }
                        break;

                    case NetworkSessionEndReason.RemovedByHost:
                        // host has kicked us from the game
                        Assert.Fatal(!NetworkSessionManager.Instance.IsHosting(), "Silliness - host has kicked the host from the game!");
                        NetworkSessionManager.Instance.ShutdownSession();
                        GameStateManager.Instance.PopOverlaysImmediately();
                        DialogNotification.Show(DialogNotification.MESSAGE_KICKEDFROMMATCH, _quitSelectionScreen);
                        break;
                }
            }
        }

        public override void MoveToCharacter(int player, TutorManager.EnumTutor character)
        {
            base.MoveToCharacter(player, character);

            // broadcast movement to all players
            if (!NetworkSessionManager.Instance.IsValidSession) return;

            NetworkMessage msg = new NetworkMessage(NetworkMessage.EnumType.SelectionMovement, new SelectionInfo(player, character));
            LocalNetworkGamer sender;

            if (NetworkSessionManager.Instance.IsHosting())
            {
                sender = (NetworkSessionManager.Instance.Session.Host as LocalNetworkGamer);
            }
            else
            {
                sender = NetworkSessionManager.Instance.Session.LocalGamers[0];
            }

            NetworkSessionManager.Instance.SendDataToAll(msg, sender);
        }

        protected override bool OnSelectionChanged(int player, TutorManager.EnumTutor character)
        {
            if (!NetworkSessionManager.Instance.IsValidSession) return false;   // if session isn't valid then selecting anything is a moot point anyway so return false

            NetworkMessage msg = new NetworkMessage(NetworkMessage.EnumType.SelectionsChanged, new SelectionInfo(player, character));

            // if we are hosting then broadcast the change
            if (NetworkSessionManager.Instance.IsHosting())
            {
                LocalNetworkGamer sender = (NetworkSessionManager.Instance.Session.Host as LocalNetworkGamer);
                NetworkSessionManager.Instance.SendDataToAll(msg, sender);
                return true;
            }
            // else send the change to the host only
            else
            {
                LocalNetworkGamer sender = NetworkSessionManager.Instance.Session.LocalGamers[0];
                NetworkSessionManager.Instance.SendDataToHost(msg, sender);
                return false;
            }
        }

        private void UpdateMovementFromNetworkData()
        {
            // update the movement as specified by the player and movement data in the network message
            SelectionInfo info = _networkMessage.Data as SelectionInfo;
            Assert.Fatal(Game.Instance.Gamertag != _networkMessage.Sender.Gamertag, "Being asked to update character selection with our own broadcast data - shouldn't happen");
            base.MoveToCharacter(info.Player, info.Character);  // call base class directly to avoid triggering broadcasting the move again.
        }

        private void UpdateSelectionsFromNetworkData()
        {
            // update selection specified by data in the network message
            SelectionInfo info = _networkMessage.Data as SelectionInfo;

            Debug.WriteLine("network data msg to change selection: " + info.Character + " for player[" + info.Player + "]");

            // if we're hosting then broadcast the change to clients
            // NOTE: we must do this before we update locally or the client would get the GotoVsScreen message
            // *before* the selection change message, which might mess things up a bit.
            if (NetworkSessionManager.Instance.IsHosting())
            {
                NetworkMessage msg = new NetworkMessage(NetworkMessage.EnumType.SelectionsChanged, new SelectionInfo(info.Player, info.Character));
                LocalNetworkGamer sender = (NetworkSessionManager.Instance.Session.Host as LocalNetworkGamer);
                NetworkSessionManager.Instance.SendDataToAll(msg, sender);
            }

            // do the actual selection locally
            DoSelectionChange(info.Player, info.Character);
        }

        private void UpdateFromWelcomePack()
        {
            object[] info = _networkMessage.Data as object[];

            string[] players = info[0] as string[];
            TutorManager.EnumTutor player1_highlighted = (TutorManager.EnumTutor)info[1];
            TutorManager.EnumTutor player2_highlighted = (TutorManager.EnumTutor)info[2];
            TutorManager.EnumTutor player1_selected = (TutorManager.EnumTutor)info[3];
            TutorManager.EnumTutor player2_selected = (TutorManager.EnumTutor)info[4];

            Game.Instance.ActiveGameplaySettings.RemoveAllPlayers();

            // NOTE: we add the gamers we are told to add because that is the state the game was in
            // when we joined the session - after this we will then be synced with that state
            // and ready to process any joined/left messages.
            for (int i = 0; i < players.Length; i++)
            {
                foreach (NetworkGamer gamer in NetworkSessionManager.Instance.Session.AllGamers)
                {
                    if (gamer.Gamertag == players[i])
                    {
                        AddNetworkGamer(gamer);
                        break;
                    }
                }
            }

            // do the setup/init stuff we didn't do when the scene first loaded (because we didn't have the data for it)
            InitializeScene();
            SetupCharacterSelectors();
            InitializeSelections();
            InitBossSelection();
            TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("selection_counter").Visible = true;
            //base.MoveToCharacter(PLAYER_1, player1_highlighted);
            //base.MoveToCharacter(PLAYER_2, player2_highlighted);
            //DoSelectionChange(PLAYER_1, player1_selected);
            //DoSelectionChange(PLAYER_2, player2_selected);

            IsJoining = false;  // we've joined the game properly now
        }

        protected override void InitializeScene()
        {
            if (!IsJoining)
            {
                base.InitializeScene();
                TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("selection_counter").Visible = true;
            }
        }

        protected override void SetupCharacterSelectors()
        {
            if (!IsJoining)
            {
                base.SetupCharacterSelectors();
            }
        }

        protected override void InitializeSelections()
        {
            if (!IsJoining)
            {
                base.InitializeSelections();
            }
        }

        protected override void InitBossSelection()
        {
            if (!IsJoining)
            {
                base.InitBossSelection();
            }
        }

        protected override void ProcessBossSelection()
        {
            if (!IsJoining)
            {
                base.ProcessBossSelection();
            }
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

        protected override bool CheckReadyToPlay()
        {
            if (!NetworkSessionManager.Instance.IsValidSession) return false;   // if session isn't valid then selecting anything is a moot point anyway so return false

            // if we are hosting then broadcast to clients that they should proceed to the vs screen
            // and also goto the vs screen ourselves (*if* all players have selected a character)
            if (NetworkSessionManager.Instance.IsHosting())
            {
                bool readyToPlay = AllPlayersReady();

                if (readyToPlay)
                {
                    NetworkMessage msg = new NetworkMessage(NetworkMessage.EnumType.GotoVsScreen, null);
                    LocalNetworkGamer sender = (NetworkSessionManager.Instance.Session.Host as LocalNetworkGamer);
                    NetworkSessionManager.Instance.SendDataToAll(msg, sender);

                    AddAsyncTask(AsyncTask_GotoVsScreen(), true);
                }

                return readyToPlay;
            }
            else
            {
                return false;
            }
        }

        protected override void OnQuitting()
        {
            base.OnQuitting();
            NetworkSessionManager.Instance.ShutdownSession();
        }

        protected override void DoQuitting()
        {
            if (GameStateVsSplashSinglePlayer.HasSavedProgress)
            {
                GameStateManager.Instance.PopOverlaysImmediately();
                DialogNotification.Show(DialogNotification.MESSAGE_RETURNINGTOCHAMPIONSHIP, _returnToChampionship);
            }
            else
            {
                base.DoQuitting();
            }
        }

        protected override void QuitSelectionScreen(int player)
        {
            // this is a LIVE session so can't quit back to the settings screen - can only quit the session
            base.QuitSelectionScreen(player);
        }

        private void QuitToLobby()
        {
            // NOTE: the isExiting check will already have been done and isExiting will be true by this point
            //if (!_isExiting)
            //{
            //    _isExiting = true;
                GameStateManager.Instance.ClearAndLoadWithPrefilledStack(GameStateNames.SETTINGS_MULTIPLAYER_LIVE, new string[] { GameStateNames.MAINMENU, GameStateNames.MULTIPLAYER, GameStateNames.XBOXLIVE }, null);
            //}
        }

        protected override void GotoVsScreen()
        {
            base.GotoVsScreen();

            // go have a math fight!...
            GameStateManager.Instance.ClearAndLoad(GameStateNames.VS_SPLASH_MULTIPLAYERLIVE, null);
        }



        public class SelectionInfo
        {
            public int Player;
            public TutorManager.EnumTutor Character;


            public SelectionInfo()
            {
            }

            public SelectionInfo(int p, TutorManager.EnumTutor t)
            {
                Player = p;
                Character = t;
            }
        }


        ////////////////////////////////////////////////////////////////////
        // private action stuff - not for selecting in the editor
        ////////////////////////////////////////////////////////////////////

        // called when we are displaying a session status dialog, such as 'connection lost' and we need
        // to immediately quit the selection screen when the player clicks 'continue' on that dialog.
        private static GUIActionDelegate _quitSelectionScreen = new GUIActionDelegate(delegate(GUIComponent guiComponent, string paramString)
        {
            if (GameStateVsSplashSinglePlayer.HasSavedProgress)
            {
                GameStateManager.Instance.PopOverlaysImmediately();
                DialogNotification.Show(DialogNotification.MESSAGE_RETURNINGTOCHAMPIONSHIP, _returnToChampionship);
            }
            else
            {
                Debug.WriteLine("(Action) quitting the selection screen");
                GameStateManager.Instance.ClearAndLoad(GameStateNames.MAINMENU, null);
            }
        });

        // called when we are displaying a notification that a non-host player has quit - so we should return to the lobby
        private void _returnToLobby(GUIComponent guiComponent, string paramString)
        {
            Debug.WriteLine("(Action) returning to lobby");
            QuitToLobby();
        }


        private static GUIActionDelegate _returnToChampionship = new GUIActionDelegate(delegate(GUIComponent guiComponent, string paramString)
        {
            Debug.WriteLine("(Action) quitting returning to championship");
            GameStateManager.Instance.ClearAndLoad(GameStateNames.VS_SPLASH_SINGLEPLAYER, null);
        });
    }
}
