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



namespace MathFreak.GameStates
{
    /// <summary>
    /// This gamestate displays the game credits
    /// </summary>
    public class GameStateCredits : GameState
    {
        public override void OnTransitionOnCompleted()
        {
            base.OnTransitionOnCompleted();

            Game.Instance.LoadScene(@"data\levels\Credits.txscene");

            GUIManager.Instance.ActivateGUI();
        }

        public override void PreTransitionOff()
        {
            base.PreTransitionOff();

            Game.Instance.UnloadScene(@"data\levels\Credits.txscene");
        }
    }
}
