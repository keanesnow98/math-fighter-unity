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
using MathFreak.GamePlay;
using System.Diagnostics;



namespace MathFreak.GameStates
{
    /// <summary>
    /// This gamestate handles Freak menu screen for setting up the Freak gameplay mode
    /// </summary>
    [TorqueXmlSchemaType]
    public class GameStateFreak : GameState
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

            Game.Instance.LoadScene(@"data\levels\Freak.txscene");

            // set up the flyon transition
            _flyonTransition = new TransitionHorizontalFlyon();
            _flyonTransition.ObjFlyonDelay = BUTTON_FLYON_DELAY;

            _flyonTransition.Add(TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("buttonfreak_play"));
            _flyonTransition.Add(TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("buttonfreak_cats"));
            _flyonTransition.Add(TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("buttonfreak_challengers"));
            _flyonTransition.Add(TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("buttonfreak_back"));
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

            _falloffTransition.Add(TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("buttonfreak_back"));
            _falloffTransition.Add(TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("buttonfreak_challengers"));
            _falloffTransition.Add(TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("buttonfreak_cats"));
            _falloffTransition.Add(TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("buttonfreak_play"));
        }                                                                                  

        public override bool TickTransitionOff(float dt)
        {
            base.TickTransitionOff(dt);

            return _falloffTransition.tick(dt);
        }

        public override void OnTransitionOffCompleted()
        {
            base.OnTransitionOffCompleted();

            Game.Instance.UnloadScene(@"data\levels\Freak.txscene");
        }

        public static GUIActionDelegate Play { get { return _play; } }

        private static GUIActionDelegate _play = new GUIActionDelegate(delegate(GUIComponent guiComponent, string paramString)
        {
            Debug.WriteLine("(Action) pushing gamestate GamePlayFreak");

            // create a game settings object for the gameplay to use
            GamePlaySettings settings = new GamePlaySettings();
            Game.Instance.ActiveGameplaySettings = settings;
            
            // Add the active player to the game settings
            settings.AddPlayer(Game.Instance.GetLocalPlayer());

            // Setup a question dealer and assign it to the game settings
            //settings.Dealer = new QuestionDealer(3, 1);

            // Play ball!!!
            GameStateManager.Instance.Push(GameStateNames.GAMEPLAY_FREAK, null);
        });
    }
}
