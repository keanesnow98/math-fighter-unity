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
using MathFreak.ContentLoading;
using MathFreak.Text;
using Microsoft.Xna.Framework.Graphics;



namespace MathFreak.GameStates
{
    /// <summary>
    /// This is the base class for the Vs Screen gamestate - most of the functionality is in this class,
    /// but derived classes might add some extra displayable info and at the very least will load the
    /// appropriate gameplay state for their gameplay mode.
    /// </summary>
    [TorqueXmlSchemaType]
    public abstract class GameStateVsSplash : GameState
    {
        protected const String TASKLIST_VSSCREEN_ASYNC_EVENTS = "characterselectionasyncevents";
        protected float _dt;

        protected static ContentBlock _contentBlock;


        public override void  Init()
        {
            base.Init();

            AsyncTaskManager.Instance.NewTaskList(TASKLIST_VSSCREEN_ASYNC_EVENTS);
        }

        public override void OnTransitionOnCompleted()
        {
            base.OnTransitionOnCompleted();

            //// TESTING - unload content that we need but is still registered with another loader (*should* be automagically loaded for us)
            //Game.Instance.RemoveContentLoader("Default");

            // stop the music!  everybody lie down on the floor and stay calm...
            MFSoundManager.Instance.StopMusic();

            // note: loading the scene *after* transitioning the other screen off and unloaded it so we don't overlap with the other scene
            Game.Instance.LoadScene(@"data\levels\vs_screen.txscene");
            Game.Instance.LoadScene(@"data\levels\vs_screen_ExtraTextures.txscene");

            InitializeScene();

            //GUIManager.Instance.ActivateGUI();

            // kick off the visual effects/anims
            AddAsyncTask(AsyncTask_CharacterSlideOn(), false);

            MFSoundManager.Instance.PlaySFX(MFSoundManager.EnumSFX.VsScreen);
        }

        // derived classes can do any extra scene intialization here
        protected virtual void InitializeScene()
        {
            TutorManager.Instance.CacheAnims();

            T2DStaticSprite character1 = TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("vs_character1");
            T2DStaticSprite character2 = TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("vs_character2");

            character1.Visible = false;
            character2.Visible = false;
            character1.Material = TutorManager.Instance.GetVsSprite(0, Game.Instance.ActiveGameplaySettings.Players[0].Character);
            character2.Material = TutorManager.Instance.GetVsSprite(1, Game.Instance.ActiveGameplaySettings.Players[1].Character);

            TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("vs_centertext").Visible = false;
            TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("vs_raysforeground").Visible = false;
            TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("vs_raysbackground").Visible = false;
            TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("vs_yellowraysforeground").Visible = false;
            TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("vs_yellowraysbackground").Visible = false;

            T2DSceneObject playername1 = TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("vs_playername1");
            T2DSceneObject playername2 = TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("vs_playername2");

            playername1.Visible = false;
            playername2.Visible = false;
            playername1.Components.FindComponent<MathMultiTextComponent>().TextValue = Game.Instance.ActiveGameplaySettings.Players[0].GamerTag;
            playername2.Components.FindComponent<MathMultiTextComponent>().TextValue = Game.Instance.ActiveGameplaySettings.Players[1].GamerTag;
        }

        public override void PreTransitionOff()
        {
            base.PreTransitionOff();

            AsyncTaskManager.Instance.KillAllTasks(TASKLIST_VSSCREEN_ASYNC_EVENTS, true);
            Game.Instance.UnloadScene(@"data\levels\vs_screen_ExtraTextures.txscene");
            Game.Instance.UnloadScene(@"data\levels\vs_screen.txscene");
        }

        public override void Tick(float dt)
        {
            base.Tick(dt);

            _dt = dt;

            // tick the async events list
            AsyncTaskManager.Instance.Tick(TASKLIST_VSSCREEN_ASYNC_EVENTS);
        }

        protected void AddAsyncTask(IEnumerator<AsyncTaskStatus> task, bool startImmediately)
        {
            AsyncTaskManager.Instance.AddTask(task, TASKLIST_VSSCREEN_ASYNC_EVENTS, startImmediately);
        }

        protected virtual void GotoGameplay()
        {
        }

        public static void UnloadContent()
        {
            _contentBlock.UnloadImmediately();
            _contentBlock.Dispose();
            _contentBlock = null;
        }

        protected IEnumerator<AsyncTaskStatus> AsyncTask_CharacterSlideOn()
        {
            // position the characters offscreen ready to slide on
            T2DStaticSprite character1 = TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("vs_character1");
            T2DStaticSprite character2 = TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("vs_character2");

            float xTarget = character2.Position.X;
            float xStart = 850.0f;
            float xDist = xStart - xTarget;

            character1.Position = new Vector2(-xStart, character1.Position.Y);
            character2.Position = new Vector2(xStart, character2.Position.Y);
            character1.Visible = true;
            character2.Visible = true;
            yield return null;

            // slide the characters onto the screen
            float elapsedTime = 0.0f;
            float slideDuration = 0.4f; // how long the slide on should take (in seconds)

            while (elapsedTime <= slideDuration)
            {
                character1.Position = new Vector2(-xStart + (xDist * (elapsedTime / slideDuration)), character1.Position.Y);
                character2.Position = new Vector2(xStart - (xDist * (elapsedTime / slideDuration)), character2.Position.Y);
                elapsedTime += _dt;
                yield return null;
            }

            // make sure the characters end up at precisely the right position at the end of the slide
            character1.Position = new Vector2(-xTarget, character1.Position.Y);
            character2.Position = new Vector2(xTarget, character2.Position.Y);

            // show the player names
            TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("vs_playername1").Visible = true;
            TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("vs_playername2").Visible = true;

            // trigger the other fx
            AddAsyncTask(AsyncTask_ExplodeRays(), false);
            AddAsyncTask(AsyncTask_ExplodeVs(), false);
        }

        protected IEnumerator<AsyncTaskStatus> AsyncTask_ExplodeRays()
        {
            // setup
            T2DStaticSprite fgRay = TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("vs_raysforeground");
            T2DStaticSprite bgRay = TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("vs_raysbackground");
            T2DStaticSprite fgYellowRay = TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("vs_yellowraysforeground");
            T2DStaticSprite bgYellowRay = TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("vs_yellowraysbackground");

            float targetWidth = fgRay.Size.X;
            float targetWidthYellow = fgYellowRay.Size.X;

            fgRay.Visible = true;
            bgRay.Visible = true;
            fgYellowRay.Visible = true;
            bgYellowRay.Visible = true;
            fgRay.Size = new Vector2(0.0f, 0.0f);
            bgRay.Size = new Vector2(0.0f, 0.0f);
            fgYellowRay.Size = new Vector2(0.0f, 0.0f);
            bgYellowRay.Size = new Vector2(0.0f, 0.0f);

            yield return null;

            // explode them
            float scaleWidth = 0.1f;

            // ...expand
            while (scaleWidth <= 1.5f)
            {
                fgRay.Size = new Vector2(targetWidth * scaleWidth);
                bgRay.Size = new Vector2(targetWidth * scaleWidth);
                fgYellowRay.Size = new Vector2(targetWidthYellow * scaleWidth);
                bgYellowRay.Size = new Vector2(targetWidthYellow * scaleWidth);
                scaleWidth *= 1.10f;
                yield return null;
            }

            // ...shrink
            fgRay.Layer = bgRay.Layer - 1;  // put foreground layer to back - was only in foreground for initial impact (displays over the top of the characters which creates a different effect)
            fgYellowRay.Layer = bgYellowRay.Layer - 1;  // put foreground layer to back - was only in foreground for initial impact (displays over the top of the characters which creates a different effect)

            while (scaleWidth >= 1.0f)
            {
                fgRay.Size = new Vector2(targetWidth * scaleWidth);
                bgRay.Size = new Vector2(targetWidth * scaleWidth);
                fgYellowRay.Size = new Vector2(targetWidthYellow * scaleWidth);
                bgYellowRay.Size = new Vector2(targetWidthYellow * scaleWidth);
                scaleWidth *= 0.995f;
                yield return null;
            }
        }

        protected IEnumerator<AsyncTaskStatus> AsyncTask_ExplodeVs()
        {
            // setup
            T2DStaticSprite vs = TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("vs_centertext");

            // do a grow/shrink effect
            Vector2 baseSize = vs.Size;
            vs.Size = new Vector2(1, 1);
            vs.Visible = true;
            float targetSizeY = baseSize.Y * 1.5f;
            float mul = 0.1f;

            while (vs.Size.Y < targetSizeY)
            {
                mul *= 1.101f;
                vs.Size = baseSize * mul;
                yield return null;
            }

            targetSizeY = baseSize.Y;

            while (vs.Size.Y > targetSizeY)
            {
                mul *= 0.99f;
                vs.Size = baseSize * mul;
                yield return null;
            }

            vs.Size = baseSize;    // makes sure it ends up the right size at the end of the effect

            // kick off loading art assets for the tutors - we never know what tutor combination there will be so we dynamically create a new content block each time
            _contentBlock = new ContentBlock();
            _contentBlock.AddFolder<Texture2D>(TutorManager.Instance.GetAssetPath(0, Game.Instance.ActiveGameplaySettings.Players[0].Character), false);
            _contentBlock.AddFolder<Texture2D>(TutorManager.Instance.GetAssetPath(0, Game.Instance.ActiveGameplaySettings.Players[1].Character), false);
            _contentBlock.AllowLoading();
            _contentBlock.AsyncLoadImmediately(null, null);

            // pause for a while (at minimum we need to wait until the assets have finished loading)
            // and then launch the actual gameplay
            float elapsedTime = 0.0f;

            while (elapsedTime <= 4.0f || !_contentBlock.AsyncResult.IsCompleted)
            {
                elapsedTime += _dt;
                yield return null;
            }

            GotoGameplay();
        }
    }
}
