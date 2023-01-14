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



namespace MathFreak.GamePlay
{
    /// <summary>
    /// This state handles the multiplayer specific stuff for a multiplayer game with local players.
    /// </summary>
    public class GameStateGameplayMultiplayerLocal : GameStateGameplayMultiplayer
    {
        protected override void ProcessOutro()
        {
            //if (_isExiting) return;

            //_isExiting = true;

            // clear and load the settings-lobby screen with the appropriate screens pushed under it on the stack
            UnloadAssets();
            GameStateManager.Instance.ClearAndLoadWithPrefilledStack(GameStateNames.SETTINGS_MULTIPLAYER_LOCAL, new string[] { GameStateNames.MAINMENU }, null);
        }

        protected override void PlayWinLoseSFX()
        {
            MFSoundManager.Instance.PlaySFX(MFSoundManager.EnumSFX.YouWin);
        }

        protected override void OnGamerLeft(SessionEventInfo eventInfo)
        {
            base.OnGamerLeft(eventInfo);

            // if either player has signed out then we'll need to quit this game
            if (NetworkSessionManager.Instance.IsValidSession && NetworkSessionManager.Instance.Session.AllGamers.Count < 2)
            {
                if (!_isExiting)
                {
                    _isExiting = true;
                    GameStateManager.Instance.ClearAndLoad(GameStateNames.MAINMENU, null);
                }
            }
        }

        public override void OnQuitting()
        {
            if (!_isExiting)
            {
                if (NetworkSessionManager.Instance.IsActiveSession)
                {
                    NetworkSessionManager.Instance.ShutdownSession();
                }

                base.OnQuitting();
            }
        }

        protected override bool AllGamepadsAreConnected()
        {
            bool playerOneConnected = GamePad.GetState((Game.Instance.ActiveGameplaySettings.Players[0] as PlayerLocal).XNAPlayerIndex).IsConnected;
            bool playerTwoConnected = GamePad.GetState((Game.Instance.ActiveGameplaySettings.Players[1] as PlayerLocal).XNAPlayerIndex).IsConnected;
            return playerOneConnected && playerTwoConnected;
        }
    }
}
