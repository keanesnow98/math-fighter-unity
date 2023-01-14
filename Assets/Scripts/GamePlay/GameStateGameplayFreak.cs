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
using MathFreak.Text;
using Microsoft.Xna.Framework.Graphics;



namespace MathFreak.GamePlay
{
    /// <summary>
    /// This state handles the Freak mode single player gameplay specifics.
    /// </summary>
    public class GameStateGameplayFreak : GameStateGameplaySinglePlayer
    {
        // TESTING
        //private bool isQuestionPaused = false;
        //private float isQuestionPausedButtonPressDelay = 0.0f;
        // TESTING ends

        protected override void ProcessState()
        {
            // TESTING
            // player can pause the question presentation so we can ponder the question being presented
            //isQuestionPausedButtonPressDelay += _dt;

            //if (isQuestionPausedButtonPressDelay > 0.5f)
            //{
            //    if (Game.Instance.ActiveGameplaySettings.Players[0].FreezePressed)
            //    {
            //        isQuestionPaused = !isQuestionPaused;
            //        isQuestionPausedButtonPressDelay = 0.0f;
            //    }
            //}

            //if (isQuestionPaused) return;
            // TESTING ENDS

            base.ProcessState();
        }

        protected override void DoGameEnded()
        {
            Assert.Fatal(GameEndedConditionMet(), "Freak mode: DoGameEnded() called before game-end condition was met");

            // move to the Win gameplay state (can't 'lose' Freak mode - the aim is to score points, pure and simple)
            _state = EnumGameplayState.GameWon;

            // a short pause first though
            AddAsyncTask(AsyncTask_Wait(0.5f), true);
        }

        protected override void ProcessGameWon()
        {
            if (_isExiting) return;

            _isExiting = true;

            base.ProcessGameWon();

            // do the tutor's win animation
            //AddAsyncTask(TutorManager.Instance.PlayAnim(_gameplayData.Question.Tutor, TutorManager.Tutor.EnumAnim.Win, _tutorSprite), true);

            // show message - GAME OVER!
            T2DSceneObject messageObj = TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("hud_messagetext");
            MathMultiTextComponent messageText = messageObj.Components.FindComponent<MathMultiTextComponent>();
            messageObj.Visible = true;
            messageObj.Position = Vector2.Zero;
            messageText.TextValue = "Game Over!";

            // move to the outro gameplay state after a short delay
            _state = EnumGameplayState.Outro;
            AddAsyncTask(AsyncTask_Wait(3.0f), true);
        }

        protected IEnumerator<AsyncTaskStatus> AsyncTask_DoExtraLifeFX()
        {
            // create a floater at the appropriate location
            T2DStaticSprite floater = TorqueObjectDatabase.Instance.CloneObject<T2DStaticSprite>("hud_lifefloater");
            MathMultiTextComponent text = floater.Components.FindComponent<MathMultiTextComponent>();
            text.TextValue = "EXTRA&nbspLife!";
            text.RGBA = new Vector4(0, 0.5f, 0, 1);
            Assert.Fatal(floater.Folder == null, "floater object is already in a Folder");
            floater.Folder = _sceneFolder;   // must create this object in our folder and not any overlayed scene's folder or when that scene is unloaded so is this object!
            TorqueObjectDatabase.Instance.Register(floater);

            yield return null;  // give the text component time to create it's texture

            // get the floater ready to float
            Vector2 baseSize = floater.Size;
            floater.Visible = true;

            SimpleMaterial material = (floater.Material as SimpleMaterial);
            material.IsColorBlended = true;
            material.IsTranslucent = true;

            // floater floats upwards whilst growing and fading out - anim ends when floater transparency reaches a low enough alpha level
            Vector2 posInc = new Vector2(0.0f, -0.5f);

            while (floater.VisibilityLevel >= 0.1f)
            {
                floater.VisibilityLevel -= 0.005f;
                floater.Size *= 1.001f;
                floater.Position += posInc;
                yield return null;
            }

            floater.MarkForDelete = true;   // we're done with the floater now
        }
    }
}
