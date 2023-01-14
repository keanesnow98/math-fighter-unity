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
using GarageGames.Torque.MathUtil;
using MathFreak.AsyncTaskFramework;
using MathFreak.GameStates.Dialogs;



namespace MathFreak.GameStates
{
    /// <summary>
    /// Local multiplayer version of the Vs splash screen
    /// </summary>
    [TorqueXmlSchemaType]
    public class GameStateVsSplashMultiplayerLocal : GameStateVsSplash
    {
        protected override void GotoGameplay()
        {
            // Play ball!!!
            GameStateManager.Instance.ClearAndLoad(GameStateNames.GAMEPLAY_MULTIPLAYER_LOCAL, null);
        }
    }
}
