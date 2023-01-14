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



namespace MathFreak.GameStates
{
    /// <summary>
    /// This gamestate handles the game settings screen for the single player game mode
    /// </summary>
    [TorqueXmlSchemaType]
    public class GameStateSettingsSinglePlayer : GameStateSettings
    {
        public override void OnTransitionOnCompleted()
        {
            base.OnTransitionOnCompleted();

#if XBOX
            // can only accept challengers if we can play online
            if (!(Game.Instance.GetLocalPlayer().GamerRef as SignedInGamer).Privileges.AllowOnlineSessions)
            {
                TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("buttonsettings_acceptchallengers").Components.FindComponent<GUIComponent>().Enabled = false;
            }
            else
            {
                TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("buttonsettings_acceptchallengers").Components.FindComponent<GUIComponent>().Enabled = true;
            }
#endif

            // can't select 'location' in a single player game (location depends on the AI character)
            TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("buttonsettings_location").Components.FindComponent<GUIComponent>().Enabled = false;
        }

        protected override GUIComponent GetDefaultFocusedGUIComponent()
        {
#if XBOX
            // can only accept challengers if we can play online
            if (!(Game.Instance.GetLocalPlayer().GamerRef as SignedInGamer).Privileges.AllowOnlineSessions)
            {
                return TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("buttonsettings_mathgrade").Components.FindComponent<GUIComponent>();
            }
            else
            {
                return TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("buttonsettings_acceptchallengers").Components.FindComponent<GUIComponent>();
            }
#else
            return TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("buttonsettings_acceptchallengers").Components.FindComponent<GUIComponent>();
#endif
        }

        protected override void InitializeSettings()
        {
            Game.Instance.ActiveGameplaySettings = new GamePlaySettings(Game.Instance.LastChampionshipSettings);
            Game.Instance.ActiveGameplaySettings.Location = TutorManager.EnumTutor.None;  // always 'none' for singleplayer game - make sure we override any settings saved after playing in singleplayer as AI will change location setting automatically
            Game.Instance.ActiveGameplaySettings.HasReplayed = false;   // make absolutely sure this is reset incase it got saved accidentally!!!
            Game.Instance.ActiveGameplaySettings.AddPlayer(Game.Instance.GetLocalPlayer());

            base.InitializeSettings();
        }

        protected override List<Player> GetPlayerList()
        {
            return Game.Instance.ActiveGameplaySettings.Players;
        }

        protected override void UpdateReadyStatuses()
        {
            _readyStatus[0] = (Game.Instance.ActiveGameplaySettings.Players[0] as PlayerLocal).IsReady;
        }

        protected override void OnAction_AllowChallengersToggle()
        {
            Game.Instance.ActiveGameplaySettings.AllowChallengers = !Game.Instance.ActiveGameplaySettings.AllowChallengers;
            OnSettingsChanged();
            UpdateSettingsDisplays();
        }

        protected override void GotoSelectionScreen()
        {
            base.GotoSelectionScreen();

            // store settings as 'most recently used' (these will be saved to disk when player returns to dashboard so that last settings are remembered)
            Game.Instance.LastChampionshipSettings = new GamePlaySettings(Game.Instance.ActiveGameplaySettings);
            Game.Instance.LastChampionshipSettings.RemoveAllPlayers();
            Game.Instance.LastChampionshipSettings.ResetScoreData();

            // Add the AI player and continue to the character selection screen
            Game.Instance.ActiveGameplaySettings.AddPlayer(new PlayerLocalAI(-1, Game.Instance.ActiveGameplaySettings.DifficultyLevel));

            GameStateManager.Instance.ClearAndLoad(GameStateNames.CHARACTERSELECTION_SINGLEPLAYER, null);
        }

        protected override int MinPlayersRequired()
        {
            return 1;
        }
    }
}
