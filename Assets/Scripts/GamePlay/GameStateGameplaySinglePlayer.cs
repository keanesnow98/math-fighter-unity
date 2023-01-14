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
using MathFreak.Text;
using MathFreak.AsyncTaskFramework;
using Microsoft.Xna.Framework.Net;
using MathFreak.GameStates;
using Microsoft.Xna.Framework.GamerServices;



namespace MathFreak.GamePlay
{
    /// <summary>
    /// This state handles the single player specific elements of gameplay that are shared across
    /// the different single player gameplay modes.
    /// 
    /// NOTE: this gamestate should not be registered with the gamestate manager; it is the shared
    /// base class that the specific single player gamestates will derive from, but not a gamestate
    /// that should be used directly.
    /// </summary>
    public abstract class GameStateGameplaySinglePlayer : GameStateGameplay
    {
        protected int _lives;


        protected override bool GameEndedConditionMet()
        {
            Assert.Fatal(_lives >= 0, "Freak mode: number of lives is less than zero");
            return (_lives <= 0);
        }

        protected override void SetupPlayerInput()
        {
            // hook the player up to the first player input component
            PlayerInputComponent inputComponent = TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("hud_playerinput_0").Components.FindComponent<PlayerInputComponent>();
            inputComponent.SetupInputMap(Game.Instance.ActiveGameplaySettings.Players[0] as PlayerLocal);
        }
    }
}
