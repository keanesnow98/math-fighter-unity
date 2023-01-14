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
using Microsoft.Xna.Framework.Graphics;
using System.Diagnostics;
using MathFreak.GameStates.Dialogs;



namespace MathFreak.GameStates
{
    /// <summary>
    /// This gamestate handles the multiplayer game lobby
    /// </summary>
    [TorqueXmlSchemaType]
    public class GameStateLobby : GameState
    {
        // certain session events can lead to us deciding to exit the lobby (e.g. session ended by host, or connection lost)
        // if this is the case then some stuff needs to know *not* to execute
        private static bool _isExitingLobby;

        public override void Init()
        {
            base.Init();

            // nothing to do here yet...
        }

        public override void PreTransitionOn(string paramString)
        {
            base.PreTransitionOn(paramString);

            _isExitingLobby = false;

            Game.Instance.LoadScene(@"data\levels\Lobby.txscene");

            // hide ready status art
            TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("lobby_readystatus0").Visible = false;
            TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("lobby_readystatus1").Visible = false;
        }

        public override void OnTransitionOnCompleted()
        {
            base.OnTransitionOnCompleted();

            // process any events from the network session (e.g. perhaps a player signed out, or maybe we lost the connnection during the transition to the lobby gamestate)
            ProcessSessionEvents();

            // if we are exiting the lobby then don't do anything
            if (_isExitingLobby) return;

            // if this is a local match then we should revert to the previous gamestate if there are not enough players in the session - this shouldn't really be able to happen, but checking it anyway as no *garrauntees*
            // (NOTE: if LIVE multiplayer then it's okay to only have one player in the session at this point and be waiting for others to join)
            if (ParamString == GameStateNames.GAMEPLAY_MULTIPLAYER_LOCAL && NetworkSessionManager.Instance.Session.LocalGamers.Count < 2)
            {
                // no popup message required - they are local players so everyone already knows who is signed in or not
                GameStateManager.Instance.Pop();
                return;
            }

            // setup the scene
            UpdatePlayerListAndDisplay();

            // activate the standard GUI handling stuff
            GUIManager.Instance.ActivateGUI();
        }

        public override void OnTransitionOffCompleted()
        {
            base.OnTransitionOffCompleted();

            Game.Instance.UnloadScene(@"data\levels\Lobby.txscene");
        }

        public override void OnSetAsForeground()
        {
            base.OnSetAsForeground();

            // if we are exiting then deactivate the GUI and revert to the previous gamestate
            if (_isExitingLobby)
            {
                GUIManager.Instance.DeactivateGUI();
                GameStateManager.Instance.Pop();
            }
        }

        public override void Tick(float dt)
        {
            base.Tick(dt);

            // process any events from the network session
            ProcessSessionEvents();

            // if we are exiting the lobby then don't do anything
            if (_isExitingLobby) return;

            // silliness check - if there is no active session for some reason then don't do anything
            if (!NetworkSessionManager.Instance.IsValidSession) return;


#if XBOX
            // update visual representation of ready status for all players
            List<Player> players = Game.Instance.ActiveGameplaySettings.Players;

            for (int i = 0; i < players.Count; i++)
            {
                foreach (NetworkGamer gamer in NetworkSessionManager.Instance.Session.AllGamers)
                {
                    if (gamer.Gamertag == players[i].GamerTag)
                    {
                        TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("lobby_readystatus" + i).Visible = gamer.IsReady;
                        break;
                    }
                }
            }

            // if there are enough players and they are ready then it's time to play ball!!!
            if (NetworkSessionManager.Instance.Session.AllGamers.Count == 2 && NetworkSessionManager.Instance.Session.IsEveryoneReady)
            {
                GameStateManager.Instance.Push(ParamString, null);
            }
#endif
        }

        public void ToggleReadyStatusForPlayer(int playerIndex)
        {
            // find the gamer in the session and toggle their ready status
            foreach (LocalNetworkGamer gamer in NetworkSessionManager.Instance.Session.LocalGamers)
            {
                if (gamer.Gamertag == SignedInGamer.SignedInGamers[(PlayerIndex)playerIndex].Gamertag)
                {
                    // toggle the ready status
                    gamer.IsReady = !gamer.IsReady;
                    break;
                }
            }
        }

        private void ProcessSessionEvents()
        {
            // if the session isn't valid then don't process the events as we already shutdown the session
            if (!NetworkSessionManager.Instance.IsActiveSession)
            {
                Debug.WriteLine("Lobby::ProcessSessionEvents() - session is no longer valid - ignoring events");
                return;
            }

            // pull events from the queue and process them - we stop processing events if we hit one that triggers quitting the lobby
            while (NetworkSessionManager.Instance.EventCount > 0)
            {
                // first check if we are exiting the lobby - if we are then don't process any more events
                if (_isExitingLobby) break;

                // else grab the next event from the queue
                SessionEventInfo eventInfo = NetworkSessionManager.Instance.GetNextEvent();

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

        private void OnGamerLeft(SessionEventInfo eventInfo)
        {
            // if this is a local match then we should revert to the previous gamestate if there are not enough players in the session
            if (ParamString == GameStateNames.GAMEPLAY_MULTIPLAYER_LOCAL && NetworkSessionManager.Instance.Session.LocalGamers.Count < 2)
            {
                // no popup message required - they are local players so everyone already knows who is signed in or not
                GameStateManager.Instance.Pop();
                _isExitingLobby = true; // let other lobby code know we're outta here!
            }
            // else update the player list
            else
            {
                UpdatePlayerListAndDisplay();
            }
        }

        private void OnGamerJoined(SessionEventInfo eventInfo)
        {
            UpdatePlayerListAndDisplay();
        }

        private void OnSessionEnded(SessionEventInfo eventInfo)
        {
#if XBOX
            // whatever happens here we *will* be exiting the lobby!
            _isExitingLobby = true;

            List<Player> players = Game.Instance.ActiveGameplaySettings.Players;

            // if we are the host then we just revert to the previous gamestate
            if (NetworkSessionManager.Instance.Session.Host.Gamertag == players[0].GamerRef.Gamertag)
            {
                GameStateManager.Instance.Pop();
            }
            // else we should show the appropriate popup
            else
            {
                switch ((eventInfo.Args as NetworkSessionEndedEventArgs).EndReason)
                {
                    case NetworkSessionEndReason.ClientSignedOut:
                        // just return to the mainmenu - no popup required (the player signed out so they don't need to be told by us that they signed out!)
                        NetworkSessionManager.Instance.ShutdownSession();
                        GameStateManager.Instance.Pop();
                        break;

                    case NetworkSessionEndReason.Disconnected:
                        // lost our connection to the game
                        NetworkSessionManager.Instance.ShutdownSession();
                        DialogNotification.Show(DialogNotification.MESSAGE_CONNECTIONLOST, _hideDialogAndQuitLobby);
                        break;

                    case NetworkSessionEndReason.HostEndedSession:
                        // host has ended the session
                        NetworkSessionManager.Instance.ShutdownSession();
                        DialogNotification.Show(DialogNotification.MESSAGE_HOSTENDEDMATCH, _hideDialogAndQuitLobby);
                        break;

                    case NetworkSessionEndReason.RemovedByHost:
                        // host has kicked us from the game
                        NetworkSessionManager.Instance.ShutdownSession();
                        DialogNotification.Show(DialogNotification.MESSAGE_KICKEDFROMMATCH, _hideDialogAndQuitLobby);
                        break;
                }
            }
#endif
        }

        private void UpdatePlayerListAndDisplay()
        {
            // clear/reset everything

            // clear the list
            Game.Instance.ActiveGameplaySettings.Players.Clear();

            // clear all the input component stuff
            TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("lobby_playerinput_0").Components.FindComponent<ReadyToStartComponent>().ClearInputMap();
            TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("lobby_playerinput_1").Components.FindComponent<ReadyToStartComponent>().ClearInputMap();

            // hide ready status art
            TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("lobby_readystatus0").Visible = false;
            TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("lobby_readystatus1").Visible = false;

            // set gamer pics to default
            TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("lobby_gamerpic0").Material = TorqueObjectDatabase.Instance.FindObject<RenderMaterial>("GGLogoMaterial");
            TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("lobby_gamerpic1").Material = TorqueObjectDatabase.Instance.FindObject<RenderMaterial>("GGLogoMaterial");


            // rebuild/setup everything

            // rebuild the list
            AddHostToPlayerList();
            AddOthersToPlayerList();

            // we need to listen to see if players are ready or not - so we need to hook
            // local players up to an input component
            List<Player> players = Game.Instance.ActiveGameplaySettings.Players;

            // for each local player hook them up to the corresponding input component
            for (int i = 0; i < players.Count; i++)
            {
                Player player = players[i];

                if (player is PlayerLocal)
                {
                    ReadyToStartComponent inputComponent = TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("lobby_playerinput_" + i).Components.FindComponent<ReadyToStartComponent>();
                    //inputComponent.SetupInputMap(this, (players[i] as PlayerLocal).PlayerIndex);
                }
            }

            // grab the gamer pics and display them
            for (int i = 0; i < players.Count; i++)
            {
                T2DStaticSprite sprite = TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("lobby_gamerpic" + i);
                Texture2D gamerPic = players[i].GamerPic;

                if (gamerPic != null)
                {
                    SimpleMaterial material = new SimpleMaterial();
                    material.SetTexture(gamerPic);
                    sprite.Material = material;
                }
            }

            // NOTE: ready status will get updated in the tick() method
        }

        private void AddHostToPlayerList()
        {
            AddGamertoPlayerList(NetworkSessionManager.Instance.Session.Host);
        }

        private void AddOthersToPlayerList()
        {
            GamePlaySettings settings = Game.Instance.ActiveGameplaySettings;
            NetworkSession session = NetworkSessionManager.Instance.Session;

            // add all the players that aren't the host, in the order that they appear in the list of gamers
            foreach (NetworkGamer gamer in session.AllGamers)
            {
                if (gamer.Gamertag != session.Host.Gamertag)
                {
                    AddGamertoPlayerList(gamer);
                }
            }
        }

        private void AddGamertoPlayerList(NetworkGamer gamer)
        {
            Debug.WriteLine("Gamer[" + gamer.Gamertag + "] is " + ((gamer is LocalNetworkGamer) ? "local" : "remote"));

            GamePlaySettings settings = Game.Instance.ActiveGameplaySettings;
            NetworkSession session = NetworkSessionManager.Instance.Session;

            // if the gamer is local then create a local player instance
            foreach (LocalNetworkGamer localGamer in session.LocalGamers)
            {
                if (localGamer.Gamertag == gamer.Gamertag)
                {
                    SignedInGamer signedInGamer = Game.Instance.FindSignedInGamer(gamer.Gamertag);

                    Assert.Fatal(signedInGamer != null, "AddGamertoPlayerList() - signed in gamer not found: " + gamer.Gamertag);

                    if (signedInGamer != null)
                    {
                        settings.Players.Add(new PlayerLocal((int)signedInGamer.PlayerIndex));
                    }

                    return; // return early - we've done what we came here to do!
                }
            }

            // else add the gamer as a remote player
            settings.AddPlayer(new PlayerRemote(gamer));
        }

        public static GUIActionDelegate Back { get { return _back; } }

        // called when the player cancels the lobby - we'll need to close the network session
        private static GUIActionDelegate _back = new GUIActionDelegate(delegate(GUIComponent guiComponent, string paramString)
        {
            Debug.WriteLine("(Action) cancelling the lobby");

            //_isExitingLobby = true;
            //DialogNotification.Show(DialogNotification.MESSAGE_CONNECTIONLOST, _hideDialogAndQuitLobby);

            // shutdown the network session
            NetworkSessionManager.Instance.ShutdownSession();

            // return to the previous gamestate
            GameStateManager.Instance.Pop();
        });


        ////////////////////////////////////////////////////////////////////
        // private action stuff - not for selecting in the editor
        ////////////////////////////////////////////////////////////////////

        // called when we are displaying a session status dialog, such as 'connection lost' and we need
        // to immediately quit the lobby when the player clicks 'continue' on that dialog.
        private static GUIActionDelegate _hideDialogAndQuitLobby = new GUIActionDelegate(delegate(GUIComponent guiComponent, string paramString)
        {
            Debug.WriteLine("(Action) hiding active dialog and quitting the lobby");

            // shutdown the network session
            NetworkSessionManager.Instance.ShutdownSession();

            // pop the dialog
            GameStateManager.Instance.PopImmediately(null);

            // we will get notified once the dialog has been hidden - see OnSetAsForeground()
        });
    }
}
