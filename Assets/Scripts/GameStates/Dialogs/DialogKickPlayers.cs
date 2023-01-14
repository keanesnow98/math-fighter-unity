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
    /// Allows the host to kick the other player from the game.
    /// </summary>
    [TorqueXmlSchemaType]
    public class DialogKickPlayers : MathFreakUIDialog
    {
        public enum EnumResponse { None, Cancelled, KickPlayer1 };

        private static bool _responseRecieved;
        private static EnumResponse _response = EnumResponse.None;
        private static string _gamerTag;

        
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

            Game.Instance.LoadScene(@"data\levels\DialogKickPlayers.txscene");

            // hide/show the gamertag buttons
            T2DStaticSprite p1 = TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("dialogkick_player1");
            p1.Visible = true;
            p1.Components.FindComponent<MFTextMenuButton>().TextLabel = _gamerTag;
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
            Game.Instance.UnloadScene(@"data\levels\DialogKickPlayers.txscene");
        }

        public static void ResetResponse()
        {
            _responseRecieved = false;
            _response = EnumResponse.None;
        }

        public static void Show(string gamerTag)
        {
            _gamerTag = gamerTag;
            GameStateManager.Instance.PushOverlay(GameStateNames.DIALOG_KICKPLAYERS, null);
        }


        public static GUIActionDelegate KickPlayer1 { get { return _kickPlayer1; } }
        public static GUIActionDelegate Back { get { return _back; } }

        private static GUIActionDelegate _kickPlayer1 = new GUIActionDelegate(delegate(GUIComponent guiComponent, string paramString)
        {
            Debug.WriteLine("(Action) kicking player 1");
            _response = EnumResponse.KickPlayer1;
            GameStateManager.Instance.Pop();
        });

        private static GUIActionDelegate _back = new GUIActionDelegate(delegate(GUIComponent guiComponent, string paramString)
        {
            Debug.WriteLine("(Action) NOT kicking any players");
            _response = EnumResponse.Cancelled;
            GameStateManager.Instance.Pop();
        });
    }
}
