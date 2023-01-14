using System;
using System.Collections.Generic;
////using System.Linq;
using System.Text;

using GarageGames.Torque.Sim;
using GarageGames.Torque.Core;
using GarageGames.Torque.T2D;

using MathFreak.GUIFrameWork;



namespace MathFreak.GameStateFramework
{
    /// <summary>
    /// A gamestate dialog is a gamestate that acts like a popup dialog - displaying over an active
    /// gamestate rather than causing the other state to transition off.  This is achieved by setting
    /// the other gamestate as a 'background gamestate' until the dialog disappears.
    /// 
    /// TODO: add shared dialog behaviour stuff here as needed
    /// </summary>
    public abstract class GameStateDialog : GameState
    {
    }
}
