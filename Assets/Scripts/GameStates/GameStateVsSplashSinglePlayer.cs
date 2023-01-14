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
using GarageGames.Torque.MathUtil;
using MathFreak.AsyncTaskFramework;
using MathFreak.GameStates.Dialogs;



namespace MathFreak.GameStates
{
    /// <summary>
    /// Singleplayer version of the Vs splash screen
    /// </summary>
    [TorqueXmlSchemaType]
    public class GameStateVsSplashSinglePlayer : GameStateVsSplash
    {
        // stores the saved singleplayer progress so player can return to it if they are challenged to a LIVE mp match
        // NOTE: we could pretty much logicall put this variable anywhere, but put it in the Vs Screen
        // as that's the one that will be using the saved progress data to setup the game when player
        // returns from LIVE mp.
        private static GamePlaySettings _savedProgress;

        public static bool HasSavedProgress
        {
            get { return _savedProgress != null; }
        }


        public override void PreTransitionOn(string paramString)
        {
            // if we have saved progress data available then we need to setup the game using that
            if (HasSavedProgress)
            {
                Game.Instance.ActiveGameplaySettings = _savedProgress;
                ClearSavedProgress();   // don't need the saved copy anymore
            }

            base.PreTransitionOn(paramString);
        }

        public static void SaveProgress()
        {
            _savedProgress = new GamePlaySettings(Game.Instance.ActiveGameplaySettings);
        }

        public static void ClearSavedProgress()
        {
            _savedProgress = null;
        }

        protected override void GotoGameplay()
        {
            // if we are allowing challengers then save progress so if we get an mp LIVE challenge
            // we automatically have the data we need to restore the game when we return to single player
            if (Game.Instance.ActiveGameplaySettings.AllowChallengers)
            {
                SaveProgress();
            }

            // Play ball!!!
            GameStateManager.Instance.ClearAndLoad(GameStateNames.GAMEPLAY_CHAMPIONSHIP, null);
        }
    }
}
