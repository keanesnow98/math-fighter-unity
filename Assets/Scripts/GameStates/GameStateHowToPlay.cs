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



namespace MathFreak.GameStates
{
    /// <summary>
    /// This gamestate handles the highscores screen
    /// </summary>
    [TorqueXmlSchemaType]
    public class GameStateHowToPlay : GameState
    {
        private TransitionHorizontalFlyon _flyonTransition;
        private TransitionFallOff _falloffTransition;
        private const float BUTTON_FLYON_DELAY = 0.1f;
        private const float BUTTON_FALLOFF_DELAY = 0.1f;

        private const int PAGE_COUNT = 3;

        private static int _currentPage;


        public override void PreTransitionOn(string paramString)
        {
            base.PreTransitionOn(paramString);

            Game.Instance.LoadScene(@"data\levels\HowToPlay.txscene");
            Game.Instance.LoadScene(@"data\levels\HowToPlay_Extra.txscene");

            // hide the gui buttons
            TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("howtoplay_prev").Visible = false;
            TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("howtoplay_next").Visible = false;
            TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("howtoplay_back").Visible = false;

            // make sure viewing the first page
            _currentPage = 0;
            ShowCurrentPage();

            // set up the flyon transition
            _flyonTransition = new TransitionHorizontalFlyon();
            _flyonTransition.ObjFlyonDelay = BUTTON_FLYON_DELAY;

            _flyonTransition.Add(TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("howtoplay_dialog"));
        }

        public override bool TickTransitionOn(float dt, bool prevStateHasTransitionedOff)
        {
            base.TickTransitionOn(dt, prevStateHasTransitionedOff);

            if (!prevStateHasTransitionedOff) return false;

            // process the transition
            return _flyonTransition.tick(dt);
        }

        public override void OnTransitionOnCompleted()
        {
            base.OnTransitionOnCompleted();

            // show the gui buttons
            TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("howtoplay_prev").Visible = true;
            TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("howtoplay_next").Visible = true;
            TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("howtoplay_back").Visible = true;

            GUIManager.Instance.ActivateGUI();
        }

        public override void PreTransitionOff()
        {
            base.PreTransitionOff();

            // make sure if stuff is still flying on that we cancel that transition
            _flyonTransition.CancelAll();

            // hide the gui buttons
            TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("howtoplay_prev").Visible = false;
            TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("howtoplay_next").Visible = false;
            TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("howtoplay_back").Visible = false;

            // set up the fall off transition
            _falloffTransition = new TransitionFallOff();
            _falloffTransition.ObjFalloffDelay = BUTTON_FALLOFF_DELAY;

            _falloffTransition.Add(TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("howtoplay_dialog"));
        }

        public override bool TickTransitionOff(float dt)
        {
            base.TickTransitionOff(dt);

            return _falloffTransition.tick(dt);
        }

        public override void OnTransitionOffCompleted()
        {
            base.OnTransitionOffCompleted();

            Game.Instance.UnloadScene(@"data\levels\HowToPlay_Extra.txscene");
            Game.Instance.UnloadScene(@"data\levels\HowToPlay.txscene");
        }

        private static void ShowCurrentPage()
        {
            TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("howtoplay_dialog").Material = TorqueObjectDatabase.Instance.FindObject<RenderMaterial>("How_to_play_" + _currentPage);
        }

        private static void Action_Next(GUIComponent guiComponent, string paramString)
        {
            _currentPage++;

            if (_currentPage >= PAGE_COUNT)
            {
                _currentPage = 0;
            }

            ShowCurrentPage();
        }

        private static void Action_Prev(GUIComponent guiComponent, string paramString)
        {
            _currentPage--;

            if (_currentPage < 0)
            {
                _currentPage = PAGE_COUNT - 1;
            }

            ShowCurrentPage();
        }


        public static GUIActionDelegate Next { get { return Action_Next; } }
        public static GUIActionDelegate Prev { get { return Action_Prev; } }
    }
}
