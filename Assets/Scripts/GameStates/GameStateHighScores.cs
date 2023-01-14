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
using System.Diagnostics;



namespace MathFreak.GameStates
{
    /// <summary>
    /// This gamestate presents a menu so the player can decide which highscores to look at.
    /// </summary>
    [TorqueXmlSchemaType]
    public class GameStateHighScores : GameState
    {
        private TransitionHorizontalFlyon _flyonTransition;
        private TransitionFallOff _falloffTransition;
        private const float BUTTON_FLYON_DELAY = 0.1f;
        private const float BUTTON_FALLOFF_DELAY = 0.1f;

        public override void Init()
        {
            base.Init();

            // nothing to do yet
        }

        public override void PreTransitionOn(string paramString)
        {
            base.PreTransitionOn(paramString);

            FEUIbackground.Instance.Load();

            Game.Instance.LoadScene(@"data\levels\HighScores.txscene");

            // set up the flyon transition
            _flyonTransition = new TransitionHorizontalFlyon();
            _flyonTransition.ObjFlyonDelay = BUTTON_FLYON_DELAY;

            _flyonTransition.Add(TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("highscores_singleplayer"));
            _flyonTransition.Add(TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("highscores_xboxlive"));
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

            GUIManager.Instance.ActivateGUI();
        }

        public override void PreTransitionOff()
        {
            base.PreTransitionOff();

            // make sure if stuff is still flying on that we cancel that transition
            _flyonTransition.CancelAll();

            // set up the fall off transition
            _falloffTransition = new TransitionFallOff();
            _falloffTransition.ObjFalloffDelay = BUTTON_FALLOFF_DELAY;

            _falloffTransition.Add(TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("highscores_xboxlive"));
            _falloffTransition.Add(TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("highscores_singleplayer"));
        }

        public override bool TickTransitionOff(float dt)
        {
            base.TickTransitionOff(dt);

            return _falloffTransition.tick(dt);
        }

        public override void OnTransitionOffCompleted()
        {
            base.OnTransitionOffCompleted();

            Game.Instance.UnloadScene(@"data\levels\HighScores.txscene");
        }

        
        
        public static GUIActionDelegate SinglePlayer { get { return _singlePlayer; } }
        public static GUIActionDelegate XboxLIVE { get { return _xboxlive; } }

        private static GUIActionDelegate _singlePlayer = new GUIActionDelegate(delegate(GUIComponent guiComponent, string paramString)
        {
            Debug.WriteLine("(Action) pushing singleplayer highscores gamestate");
            GameStateManager.Instance.Push(GameStateNames.HIGHSCORES_SINGLEPLAYER, null);
        });

        private static GUIActionDelegate _xboxlive = new GUIActionDelegate(delegate(GUIComponent guiComponent, string paramString)
        {
            Debug.WriteLine("(Action) pushing Xbox LIVE highscores gamestate");
            GameStateManager.Instance.Push(GameStateNames.HIGHSCORES_MULTIPLAYER, null);
        });
    }
}
