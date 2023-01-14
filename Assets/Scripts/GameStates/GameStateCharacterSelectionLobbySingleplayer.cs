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
    /// The singleplayer specific version of the character selection screen
    /// </summary>
    [TorqueXmlSchemaType]
    public class GameStateCharacterSelectionLobbySingleplayer : GameStateCharacterSelectionLobby
    {
        protected List<TutorManager.EnumTutor> _unavailablecharacters;


        protected override void InitializeScene()
        {
            base.InitializeScene();

            // grey out any characters the AI has already used
            _unavailablecharacters = (Game.Instance.ActiveGameplaySettings.Players[PLAYER_2] as PlayerLocalAI).GetUnavailableCharacters();

            if (_unavailablecharacters.Contains(TutorManager.EnumTutor.Einstein))
            {
                TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("selection_headshot_einstein").Material = TorqueObjectDatabase.Instance.FindObject<RenderMaterial>("Einestein_GreyMaterial");
            }

            if (_unavailablecharacters.Contains(TutorManager.EnumTutor.Caveman))
            {
                TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("selection_headshot_caveman").Material = TorqueObjectDatabase.Instance.FindObject<RenderMaterial>("Caveman_GreyMaterial");
            }

            if (_unavailablecharacters.Contains(TutorManager.EnumTutor.SchoolTeacher))
            {
                TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("selection_headshot_teacher").Material = TorqueObjectDatabase.Instance.FindObject<RenderMaterial>("Teacher_GreyMaterial");
            }

            if (_unavailablecharacters.Contains(TutorManager.EnumTutor.Robot))
            {
                TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("selection_headshot_robot").Material = TorqueObjectDatabase.Instance.FindObject<RenderMaterial>("Robot_GreyMaterial");
            }

            if (_unavailablecharacters.Contains(TutorManager.EnumTutor.AtomicKid))
            {
                TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("selection_headshot_atomickid").Material = TorqueObjectDatabase.Instance.FindObject<RenderMaterial>("Atomic_Kid_GreyMaterial");
            }
        }

        protected override void InitializeSelections()
        {
            base.InitializeSelections();

            // TESTING - if need to test winning the championship then can uncomment this line and also comment the lines in the AI class so no characters left and AI will pick boss
            //Game.Instance.ActiveGameplaySettings.Players[PLAYER_1].Character = TutorManager.EnumTutor.Caveman;
            // TESTING ENDS
    
            // if this is not the first time through this screen in the championship playthrough then the player has already selected their
            // player and should not be allowed to select a different one now.  We can tell if this is the first time because
            // of how many characters the AI has used (i.e. none if it's the first time).
            if (_unavailablecharacters.Count != 0)
            {
                // player already has a character selected so select it for them automatically
                SelectCharacter(PLAYER_1, Game.Instance.ActiveGameplaySettings.Players[PLAYER_1].Character);
                _player1_hilightedCharacter = _player1_selectedCharacter;
                (_player2_characterSelector as PlayerLocalAICharacterSelector).EnableSelectingCharacter();
            }
        }

        protected override void SelectCharacter(int player, TutorManager.EnumTutor character)
        {
            base.SelectCharacter(player, character);

            Assert.Fatal(character != TutorManager.EnumTutor.None, "Player should not be able to deselect character in singleplayer mode");

            // if the human player has selected a character then tell the AI it can select its character now
            if (player == PLAYER_1 && character != TutorManager.EnumTutor.None)
            {
                (_player2_characterSelector as PlayerLocalAICharacterSelector).EnableSelectingCharacter();
            }
        }

        public override void MoveToCharacter(int player, TutorManager.EnumTutor character)
        {
            // player can only select a new character if this is the start of the championship
            // AI can always select
            if (_unavailablecharacters.Count == 0 || player == PLAYER_2)
            {
                base.MoveToCharacter(player, character);
            }
            else
            {
                base.MoveToCharacter(PLAYER_1, Game.Instance.ActiveGameplaySettings.Players[PLAYER_1].Character);
            }
        }

        public override void OnAction_Pressed_A(int player)
        {
            // player can only select a new character if this is the start of the championship
            // AI can always select
            if (_unavailablecharacters.Count == 0 || player == PLAYER_2)
            {
                base.OnAction_Pressed_A(player);
            }
        }

        public override void OnAction_Pressed_B(int player)
        {
            Assert.Fatal(player == PLAYER_1, "AI player should not trigger quitting the selection screen - only the human player should be able to do this");

            // in single player mode the player can only quit the screen - they cannot deselect their character once they have selected a character
            QuitSelectionScreen(player);
        }

        protected override void GotoVsScreen()
        {
            base.GotoVsScreen();

            // clear any saved progress
            GameStateVsSplashSinglePlayer.ClearSavedProgress();

            // increase the AI's difficulty level
            (Game.Instance.ActiveGameplaySettings.Players[PLAYER_2] as PlayerLocalAI).IncreaseDifficulty();

            // set location to AI's location
            Game.Instance.ActiveGameplaySettings.Location = Game.Instance.ActiveGameplaySettings.Players[PLAYER_2].Character;
            Game.Instance.ActiveGameplaySettings.UpdateLocationToUse();

            // go have a math fight!...
            GameStateManager.Instance.ClearAndLoad(GameStateNames.VS_SPLASH_SINGLEPLAYER, null);
        }
    }
}
