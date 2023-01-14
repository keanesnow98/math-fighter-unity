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
using MathFreak.GamePlay;



namespace MathFreak.GameStates.Dialogs
{
    /// <summary>
    /// Dialog that asks the player to confirm quitting the game and returning to the menus
    /// and then acts accordingly.
    /// </summary>
    [TorqueXmlSchemaType]
    public class DialogQuitGameplay : MathFreakUIDialog
    {
        private static string _paramString;


        public override void PreTransitionOn(string paramString)
        {
            base.PreTransitionOn(paramString);

            _paramString = paramString;

            Game.Instance.LoadScene(@"data\levels\DialogQuitGameplay.txscene");
        }

        public override void OnTransitionOffCompleted()
        {
            base.OnTransitionOffCompleted();
            DoUnload();
        }

        public override void UnloadedImmediately()
        {
            base.UnloadedImmediately();
            DoUnload();
        }

        protected void DoUnload()
        {
            Game.Instance.UnloadScene(@"data\levels\DialogQuitGameplay.txscene");
        }

        public static GUIActionDelegate Yes { get { return _yes; } }

        // called when the player selects 'yes'
        // NOTE: when the player cancels the dialog or selects 'no' then it just closes the dialog
        private static GUIActionDelegate _yes = new GUIActionDelegate(delegate(GUIComponent guiComponent, string paramString)
        {
            Debug.WriteLine("(Action) quitting the gameplay via the quit gameplay dialog");
            (GameStateManager.Instance.GetNamedState(_paramString) as GameStateGameplay).OnQuitting();
        });
    }
}
