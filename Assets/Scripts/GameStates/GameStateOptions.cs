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
    /// This gamestate handles the options screen
    /// </summary>
    [TorqueXmlSchemaType]
    public class GameStateOptions : GameState
    {
        private static GameStateOptions _this;

        private TransitionHorizontalFlyon _flyonTransition;
        private TransitionFallOff _falloffTransition;
        private const float BUTTON_FLYON_DELAY = 0.1f;
        private const float BUTTON_FALLOFF_DELAY = 0.1f;

        private const int VOLUME_STEP = 5;

        public override void Init()
        {
            base.Init();

            Assert.Fatal(_this == null, "Should never be creating more than one instance of GameStateOptions");

            _this = this;
        }

        public override void PreTransitionOn(string paramString)
        {
            base.PreTransitionOn(paramString);

            Game.Instance.LoadScene(@"data\levels\Options.txscene");

            // setup the scene
            UpdateOptionDisplays();

            // set up the flyon transition
            _flyonTransition = new TransitionHorizontalFlyon();
            _flyonTransition.ObjFlyonDelay = BUTTON_FLYON_DELAY;

            _flyonTransition.Add(TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("buttonoptions_music"));
            _flyonTransition.Add(TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("buttonoptions_sounds"));
            _flyonTransition.Add(TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("buttonoptions_back"));
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

            _falloffTransition.Add(TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("buttonoptions_back"));
            _falloffTransition.Add(TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("buttonoptions_sounds"));
            _falloffTransition.Add(TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("buttonoptions_music"));
        }

        public override bool TickTransitionOff(float dt)
        {
            base.TickTransitionOff(dt);

            return _falloffTransition.tick(dt);
        }

        public override void OnTransitionOffCompleted()
        {
            base.OnTransitionOffCompleted();

            Game.Instance.UnloadScene(@"data\levels\Options.txscene");
        }

        private void UpdateOptionDisplays()
        {
            TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("buttonoptions_music").Components.FindComponent<MFTextOptionButton>().TextLabel = "Music: " + MFSoundManager.Instance.MusicVolume + "%";
            TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("buttonoptions_sounds").Components.FindComponent<MFTextOptionButton>().TextLabel = "Sounds: " + MFSoundManager.Instance.SoundVolume + "%";
        }

        public static GUIActionDelegate MusicVolumeDecrease { get { return _musicVolumeDecrease; } }
        public static GUIActionDelegate MusicVolumeIncrease { get { return _musicVolumeIncrease; } }
        public static GUIActionDelegate SoundVolumeDecrease { get { return _soundVolumeDecrease; } }
        public static GUIActionDelegate SoundVolumeIncrease { get { return _soundVolumeIncrease; } }
        public static GUIActionDelegate Back { get { return _back; } }

        private static GUIActionDelegate _musicVolumeDecrease = new GUIActionDelegate(delegate(GUIComponent guiComponent, string paramString)
        {
            Debug.WriteLine("(Action) decreasing music volume");

            MFSoundManager.Instance.MusicVolume -= VOLUME_STEP;
            _this.UpdateOptionDisplays();
        });

        private static GUIActionDelegate _musicVolumeIncrease = new GUIActionDelegate(delegate(GUIComponent guiComponent, string paramString)
        {
            Debug.WriteLine("(Action) increasing music volume");

            MFSoundManager.Instance.MusicVolume += VOLUME_STEP;
            _this.UpdateOptionDisplays();
        });

        private static GUIActionDelegate _soundVolumeDecrease = new GUIActionDelegate(delegate(GUIComponent guiComponent, string paramString)
        {
            Debug.WriteLine("(Action) decreasing sound volume");

            MFSoundManager.Instance.SoundVolume -= VOLUME_STEP;
            _this.UpdateOptionDisplays();
        });

        private static GUIActionDelegate _soundVolumeIncrease = new GUIActionDelegate(delegate(GUIComponent guiComponent, string paramString)
        {
            Debug.WriteLine("(Action) increasing sound volume");

            MFSoundManager.Instance.SoundVolume += VOLUME_STEP;
            _this.UpdateOptionDisplays();
        });

        private static GUIActionDelegate _back = new GUIActionDelegate(delegate(GUIComponent guiComponent, string paramString)
        {
            Debug.WriteLine("(Action) exiting options menu");

            MFSoundManager.Instance.SaveSettings();
            GUIActions.PopGameState(guiComponent, paramString);
        });
    }
}
