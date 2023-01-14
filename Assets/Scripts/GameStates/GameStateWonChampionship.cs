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
using MathFreak.Text;



namespace MathFreak.GameStates
{
    /// <summary>
    /// This gamestate displays the win screen when for when the player has completed
    /// championship mode.
    /// </summary>
    [TorqueXmlSchemaType]
    public class GameStateWonChampionship : GameState
    {
        public override void OnTransitionOnCompleted()
        {
            base.OnTransitionOnCompleted();

            Game.Instance.LoadScene(@"data\levels\WonChampionship.txscene");

            // put the winning character on the championship cup
            TutorManager.Instance.CacheAnims();
            TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("win_character").Material = TutorManager.Instance.GetCupEmboss(0, Game.Instance.ActiveGameplaySettings.Players[0].Character);

            // update the text messages
            TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("winscreen_congrats").Components.FindComponent<MathMultiTextComponent>().TextValue = "Congratulations&nbsp" + Game.Instance.ActiveGameplaySettings.Players[0].GamerTag + "!";
            TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("winscreen_scoremsg").Components.FindComponent<MathMultiTextComponent>().TextValue = "(Score:&nbsp" + Game.Instance.ActiveGameplaySettings.GetCurrentScore(0) + ")";

            GUIManager.Instance.ActivateGUI();
        }

        public override void PreTransitionOff()
        {
            base.PreTransitionOff();
            Game.Instance.UnloadScene(@"data\levels\WonChampionship.txscene");
        }



        public static GUIActionDelegate ShowCredits { get { return _showCredits; } }

        private static GUIActionDelegate _showCredits = new GUIActionDelegate(delegate(GUIComponent guiComponent, string paramString)
        {
            Debug.WriteLine("(Action) Clearing and loading the credits screen");
            GameStateManager.Instance.ClearAndLoad(GameStateNames.CREDITS, null);
        });
    }
}
