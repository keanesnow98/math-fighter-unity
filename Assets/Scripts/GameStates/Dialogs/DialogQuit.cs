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
using MathFreak.AsyncTaskFramework;
using System.Diagnostics;



namespace MathFreak.GameStates.Dialogs
{
    /// <summary>
    /// Dialog that asks the player to confirm quitting the game and then acts accordingly.
    /// </summary>
    [TorqueXmlSchemaType]
    public class DialogQuit : MathFreakUIDialog
    {
        public override void PreTransitionOn(string paramString)
        {
            base.PreTransitionOn(paramString);

            Game.Instance.LoadScene(@"data\levels\DialogQuit.txscene");
        }

        public override void OnTransitionOffCompleted()
        {
            base.OnTransitionOffCompleted();

            Game.Instance.UnloadScene(@"data\levels\DialogQuit.txscene");
        }

        public static GUIActionDelegate Yes { get { return _yes; } }

        // called when the player selects 'yes'
        // NOTE: when the player cancels the dialog or selects 'no' then it just closes the dialog
        private static GUIActionDelegate _yes = new GUIActionDelegate(delegate(GUIComponent guiComponent, string paramString)
        {
            Debug.WriteLine("(Action) quitting the game via the quit dialog");
            Game.Instance.ExitTheGame();
        });
    }
}
