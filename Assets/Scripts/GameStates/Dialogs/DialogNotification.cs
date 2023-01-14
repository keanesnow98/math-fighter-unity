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
using MathFreak.Text;



namespace MathFreak.GameStates.Dialogs
{
    /// <summary>
    /// Dialog that notifies the player of something - the only option button is 'continue'.
    /// </summary>
    [TorqueXmlSchemaType]
    public class DialogNotification : MathFreakUIDialog
    {
        public const string MESSAGE_CONNECTIONLOST = "Connection lost";
        public const string MESSAGE_KICKEDFROMMATCH = "The host has kicked you from the match";
        public const string MESSAGE_HOSTENDEDMATCH = "The host has ended the match";
        public const string MESSAGE_COULDNOTCREATEMATCH = "Failed to create match";
        public const string MESSAGE_COULDNOTJOINMATCH = "Failed to join match";
        public const string MESSAGE_PLAYERLEFT = "The other player has left the match";
        public const string MESSAGE_PLAYERSIGNEDOUT = "You have been signed out of the match";
        public const string MESSAGE_RETURNINGTOCHAMPIONSHIP = "Returning to Championship game...";
        public const string MESSAGE_NOGAMESFOUND = "No games found";

        private static GUIActionDelegate _action;
        private static string _message;

        public static GUIActionDelegate Action
        {
            set { _action = value; }
        }

        public static string Message
        {
            set { _message = value; }
        }

        public override void PreTransitionOn(string paramString)
        {
            base.PreTransitionOn(paramString);

            Game.Instance.LoadScene(@"data\levels\DialogNotification.txscene");

            TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("dialognotification_message").Components.FindComponent<MathMultiTextComponent>().TextValue = _message;
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

        private void DoUnload()
        {
            Game.Instance.UnloadScene(@"data\levels\DialogNotification.txscene");
        }

        public static void Show(string message, GUIActionDelegate action)
        {
            _message = message;
            _action = action;

            GameStateManager.Instance.PushOverlay(GameStateNames.DIALOG_NOTIFICATION, null);
        }



        public static GUIActionDelegate Continue { get { return _continue; } }

        // called when the player selects 'continue'
        private static GUIActionDelegate _continue = new GUIActionDelegate(delegate(GUIComponent guiComponent, string paramString)
        {
            Debug.WriteLine("(Action) notification[" + _message + "] - continue");

            if (_action != null)
            {
                _action(guiComponent, null);
            }
        });
    }
}
