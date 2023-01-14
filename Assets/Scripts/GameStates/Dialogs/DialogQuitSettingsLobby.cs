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
    /// Dialog that asks the player to confirm quitting the settings screen.
    /// </summary>
    [TorqueXmlSchemaType]
    public class DialogQuitSettingsLobby : MathFreakUIDialog
    {
        public enum EnumResponse { None, Cancelled, ReturnToMenu, ReturnToChampionship };

        private static bool _responseRecieved;
        protected static EnumResponse _response = EnumResponse.None;

        
        public static bool ResponseRecieved
        {
            get { return _responseRecieved; }
        }

        public static EnumResponse Response
        {
            get { return _response; }
        }

        public override void PreTransitionOn(string paramString)
        {
            base.PreTransitionOn(paramString);

            Game.Instance.LoadScene(@"data\levels\" + GetSceneFileName() + ".txscene");
        }

        public override void OnTransitionOffCompleted()
        {
            base.OnTransitionOffCompleted();

            DoUnload();

            _responseRecieved = true;   // once we've unloaded ourselves then flag that we had a response - so no multiple popping goes on
        }

        public override void UnloadedImmediately()
        {
            base.UnloadedImmediately();

            DoUnload();
        }

        protected void DoUnload()
        {
            Game.Instance.UnloadScene(@"data\levels\" + GetSceneFileName() + ".txscene");
        }

        protected virtual string GetSceneFileName()
        {
            return "DialogQuitSettingsLobby";
        }

        public static void ResetResponse()
        {
            _responseRecieved = false;
            _response = EnumResponse.None;
        }


        public static GUIActionDelegate Yes { get { return _yes; } }
        public static GUIActionDelegate No { get { return _no; } }

        private static GUIActionDelegate _yes = new GUIActionDelegate(delegate(GUIComponent guiComponent, string paramString)
        {
            Debug.WriteLine("(Action) quitting the settings lobby");

            _response = EnumResponse.ReturnToMenu;
            GameStateManager.Instance.Pop();
        });

        private static GUIActionDelegate _no = new GUIActionDelegate(delegate(GUIComponent guiComponent, string paramString)
        {
            Debug.WriteLine("(Action) NOT quitting the settings lobby");
            
            _response = EnumResponse.Cancelled;
            GameStateManager.Instance.Pop();
        });
    }
}
