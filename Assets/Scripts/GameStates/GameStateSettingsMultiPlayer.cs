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



namespace MathFreak.GameStates
{
    /// <summary>
    /// This gamestate is the base class for the game settings screen for the multi player game modes.
    /// </summary>
    [TorqueXmlSchemaType]
    public abstract class GameStateSettingsMultiPlayer : GameStateSettings
    {
        public override void OnTransitionOnCompleted()
        {
            base.OnTransitionOnCompleted();

            TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("buttonsettings_acceptchallengers").Components.FindComponent<GUIComponent>().Enabled = false;
            TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("buttonsettings_difficultylevel").Components.FindComponent<GUIComponent>().Enabled = false;

            // change the default button (the default 'default' is greyed out so we need to choose another one).
            //GUIManager.Instance.SetDefault(TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("buttonsettings_location").Components.FindComponent<GUIComponent>());
        }

        protected override GUIComponent GetDefaultFocusedGUIComponent()
        {
            return TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("buttonsettings_location").Components.FindComponent<GUIComponent>();
        }

        public override void ToggleLocalPlayerReady(PlayerLocal player)
        {
            // our local player might be someone else's remote player so update the players ready status
            // in the network gamer list and we will pick up the status change in the next tick.
            GamerCollection<NetworkGamer> players = NetworkSessionManager.Instance.Session.AllGamers;

            for (int i = 0; i < players.Count; i++)
            {
                if (players[i].Gamertag == player.GamerTag)
                {
                    players[i].IsReady = !players[i].IsReady;
                    break;
                }
            }
        }

        public override void Tick(float dt)
        {
            if (!NetworkSessionManager.Instance.IsActiveSession) return;

            if (_isExiting) return;

            // process network events
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

            ProcessMessages();  // network session messages processed after session events as session events might mean no network messages to process anyway
            base.Tick(dt);
        }

        protected virtual void ProcessMessages()
        {
            // process any network session messages
        }

        protected virtual void OnGamerLeft(SessionEventInfo eventInfo)
        {
            UpdatePlayers();

            // NOTE: gamers can come and go in the lobby so no need to exit to main menu or anything like that
        }

        protected virtual void OnGamerJoined(SessionEventInfo eventInfo)
        {
            UpdatePlayers();
        }

        protected virtual void OnSessionEnded(SessionEventInfo eventInfo)
        {
            throw new NotImplementedException();
        }

        protected override void OnQuitting()
        {
            NetworkSessionManager.Instance.ShutdownSession();
            base.OnQuitting();
        }
    }
}
