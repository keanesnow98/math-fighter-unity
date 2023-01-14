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



namespace MathFreak.GameStates
{
    /// <summary>
    /// The local multiplayer specific version of the character selection screen
    /// </summary>
    [TorqueXmlSchemaType]
    public class GameStateCharacterSelectionLobbyMultiplayerLocal : GameStateCharacterSelectionLobby
    {
        // allow the other local player to also select Math Lord character with secret combo
        private List<Buttons> _P2comboEntered;
        private bool _P2buttonWasDown;
        private Buttons _P2buttonThatWasDown;


        protected override void  QuitSelectionScreen(int player)
        {
            // only player 1 can quit the selection screen (player 1 will be the main logged in player and player 2 is a secondary player)
            if (player == PLAYER_1)
            {
                base.QuitSelectionScreen(player);
            }
        }

        protected override void OnQuitting()
        {
            base.OnQuitting();
            NetworkSessionManager.Instance.ShutdownSession();
        }

        protected override void GotoVsScreen()
        {
            base.GotoVsScreen();

            // go have a math fight!...
            GameStateManager.Instance.ClearAndLoad(GameStateNames.VS_SPLASH_MULTIPLAYERLOCAL, null);
        }

        protected override void InitBossSelection()
        {
            base.InitBossSelection();

            _P2comboEntered = new List<Buttons>(_comboToMatch.Length);
            _P2buttonWasDown = false;

            // button/s may be down when entering the screen so need to discount that 'press' initially
            GamePadState gps = GamePad.GetState((Game.Instance.ActiveGameplaySettings.Players[1] as PlayerLocal).XNAPlayerIndex);

            for (Buttons buttonID = Buttons.DPadUp; buttonID <= Buttons.Y; buttonID = (Buttons)((int)buttonID * 2)) // buttons enum goes up as multiples of two so we're using that to loop through them quickly (note: Enum.GetValues() is not available on xbox)
            {
                if (gps.IsButtonDown(buttonID))
                {
                    _P2buttonWasDown = true;
                    _P2buttonThatWasDown = buttonID;
                    Debug.WriteLine("Button detected already pressed(P2): " + buttonID);
                    break;  // done now - we only search for the first pressed button
                }
            }
        }

        protected override void ProcessBossSelection()
        {
            base.ProcessBossSelection();

            // already entered enough button presses?
            if (_P2comboEntered.Count >= _comboToMatch.Length) return;

            // else get another one if there is one
            GamePadState gps = GamePad.GetState((Game.Instance.ActiveGameplaySettings.Players[1] as PlayerLocal).XNAPlayerIndex);

            bool buttonsAreDown = false;

            for (Buttons buttonID = Buttons.DPadUp; buttonID <= Buttons.Y; buttonID = (Buttons)((int)buttonID * 2))
            {
                // button was pressed
                if (gps.IsButtonDown(buttonID))
                {
                    buttonsAreDown = true;

                    // button is newly pressed this gametick
                    if (!_P2buttonWasDown || buttonID != _P2buttonThatWasDown)
                    {
                        _P2buttonThatWasDown = buttonID;
                        _P2comboEntered.Add(buttonID);
                        Debug.WriteLine("Entered(P2): " + buttonID);
                        break;  // done now - we only search for the first newly pressed button we come across and ignore the rest
                    }
                }
            }

            _P2buttonWasDown = buttonsAreDown;

            // if got enough button presses then check for a match
            if (_P2comboEntered.Count == _comboToMatch.Length)
            {
                for (int i = 0; i < _comboToMatch.Length; i++)
                {
                    if (_P2comboEntered[i] != _comboToMatch[i]) return;   // failed to match
                }

                // matched!
                Debug.WriteLine("combo matched! (player 2)");

                _player2_hilightedCharacter = TutorManager.EnumTutor.MathLord;
                OnAction_Pressed_A(PLAYER_2);
            }
        }
    }
}
