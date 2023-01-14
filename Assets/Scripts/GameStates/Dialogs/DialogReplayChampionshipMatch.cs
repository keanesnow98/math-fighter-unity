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
    /// Dialog that asks the player if the want to replay a championship match (because they lost or drew).
    /// </summary>
    [TorqueXmlSchemaType]
    public class DialogReplayChampionshipMatch : MathFreakUIDialog
    {
        private static bool _responseRecieved;
        private static bool _response;

        public static bool ResponseRecieved
        {
            get { return _responseRecieved; }
        }

        public static bool Response
        {
            get { return _response; }
        }


        public override void PreTransitionOn(string paramString)
        {
            base.PreTransitionOn(paramString);

            Game.Instance.LoadScene(@"data\levels\DialogReplayChampionshipMatch.txscene");
        }

        public override void OnTransitionOffCompleted()
        {
            base.OnTransitionOffCompleted();

            Game.Instance.UnloadScene(@"data\levels\DialogReplayChampionshipMatch.txscene");

            _responseRecieved = true;   // once we've unloaded ourselves then flag that we had a response - so no multiple popping goes on
        }

        public static void ResetResponse()
        {
            _responseRecieved = false;
            _response = false;
        }


        public static GUIActionDelegate Yes { get { return _yes; } }
        public static GUIActionDelegate No { get { return _no; } }

        private static GUIActionDelegate _yes = new GUIActionDelegate(delegate(GUIComponent guiComponent, string paramString)
        {
            Debug.WriteLine("(Action) replaying championship match - via returning to selection screen");

            _response = true;
            GameStateManager.Instance.Pop();
        });

        private static GUIActionDelegate _no = new GUIActionDelegate(delegate(GUIComponent guiComponent, string paramString)
        {
            Debug.WriteLine("(Action) returning to the main menu - not replaying championship match");
            
            _response = false;
            GameStateManager.Instance.Pop();
        });
    }
}
