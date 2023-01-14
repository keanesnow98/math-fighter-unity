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
    /// Dialog that extends the standard quit-settings-lobby dialog to add the option to
    /// return to single player championship game that the player left earlier.
    /// </summary>
    [TorqueXmlSchemaType]
    public class DialogQuitBackToChampionship : DialogQuitSettingsLobby
    {
        protected override string GetSceneFileName()
        {
            return "DialogQuitBackToChampionship";
        }


        public static GUIActionDelegate BackToChampionship { get { return _backToChampionship; } }

        private static GUIActionDelegate _backToChampionship = new GUIActionDelegate(delegate(GUIComponent guiComponent, string paramString)
        {
            Debug.WriteLine("(Action) quitting back to championship game we left earlier");

            _response = EnumResponse.ReturnToChampionship;
            GameStateManager.Instance.Pop();
        });
    }
}
