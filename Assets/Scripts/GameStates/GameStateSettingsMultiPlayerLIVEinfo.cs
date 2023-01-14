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
    /// This gamestate handles the 'info' version of the game settings screen.  This screen is shown
    /// to the player so they can see what settings a host has selected before the player decides to join
    /// a game.
    /// </summary>
    [TorqueXmlSchemaType]
    public class GameStateSettingsMultiPlayerLIVEinfo : GameState
    {
    }
}
