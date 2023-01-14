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
using System.IO;
using System.Diagnostics;
using Microsoft.Xna.Framework.Graphics;
using MathFreak.ContentLoading;



namespace MathFreak.GameStates
{
    /// <summary>
    /// This gamestate preloads content for the game, whilst displaying a 'loading' animation.
    /// When the content has loaded control will be passed to the first splashscreen (the iBlade splash).
    /// </summary>
    public class GameStateLoadContent : GameState
    {
        public override void Init()
        {
            base.Init();

            // nothing to do yet
        }

        public override void PreTransitionOn(string paramString)
        {
            base.PreTransitionOn(paramString);

            Game.Instance.LoadScene("data/levels/LoadContent.txscene");
        }

        public override void OnTransitionOffCompleted()
        {
            base.OnTransitionOffCompleted();

            Game.Instance.UnloadScene(@"data\levels\LoadContent.txscene");
        }

        // Polls for when the content has finished loading and then loads the next gamestate
        public override void Tick(float dt)
        {
            base.Tick(dt);

            // if the content has finished loading then delete the loading animation and go to the intro vid
            if (Game.Instance.SharedContent.AsyncResult.IsCompleted)
            {
                // TODO: the loader anim needs to be changed from the GG logo!
                T2DSceneObject loaderAnim = TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("LoaderAnim");

                loaderAnim.Visible = false;
                loaderAnim.MarkForDelete = true;

                GameStateManager.Instance.ClearAndLoad(GameStateNames.SPLASH_INTRO_VID, null);
            }
        }

    }
}
