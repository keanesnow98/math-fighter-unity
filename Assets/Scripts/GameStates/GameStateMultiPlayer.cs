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
using Microsoft.Xna.Framework.Net;
using MathFreak.GamePlay;
using MathFreak.GameStates.Dialogs;
using MathFreak.AsyncTaskFramework;
using Microsoft.Xna.Framework.GamerServices;
using MathFreak.Highscores;



namespace MathFreak.GameStates
{
    /// <summary>
    /// This gamestate handles the menu screen for selecting a multi-player game mode to play
    /// </summary>
    [TorqueXmlSchemaType]
    public class GameStateMultiPlayer : GameState
    {
        private TransitionHorizontalFlyon _flyonTransition;
        private TransitionFallOff _falloffTransition;
        private const float BUTTON_FLYON_DELAY = 0.1f;
        private const float BUTTON_FALLOFF_DELAY = 0.1f;

        private static string _lastSelectedButton;

        public override void Init()
        {
            base.Init();

            // nothing to do yet
        }

        public override void PreTransitionOn(string paramString)
        {
            base.PreTransitionOn(paramString);

            if (paramString == RESET_LASTSELECTED)
            {
                _lastSelectedButton = null;
            }

            FEUIbackground.Instance.Load();

            Game.Instance.LoadScene(@"data\levels\MultiPlayer.txscene");

            // set up the flyon transition
            _flyonTransition = new TransitionHorizontalFlyon();
            _flyonTransition.ObjFlyonDelay = BUTTON_FLYON_DELAY;

            _flyonTransition.Add(TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("buttonmultiplayer_LocalMatch"));
            _flyonTransition.Add(TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("buttonmultiplayer_XboxLIVE"));
            //_flyonTransition.Add(TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("buttonmultiplayer_back"));
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

            if (_lastSelectedButton != null)
            {
                GUIManager.Instance.SetFocus(TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>(_lastSelectedButton).Components.FindComponent<GUIComponent>());
                _lastSelectedButton = null;
            }

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

            //_falloffTransition.Add(TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("buttonmultiplayer_back"));
            _falloffTransition.Add(TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("buttonmultiplayer_XboxLIVE"));
            _falloffTransition.Add(TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("buttonmultiplayer_LocalMatch"));
        }                                                                                  

        public override bool TickTransitionOff(float dt)
        {
            base.TickTransitionOff(dt);

            return _falloffTransition.tick(dt);
        }

        public override void OnTransitionOffCompleted()
        {
            base.OnTransitionOffCompleted();

            Game.Instance.UnloadScene(@"data\levels\MultiPlayer.txscene");
        }

#if XBOX
        /// <summary>
        /// Will create a local session (local multiplayer is basically the same as LIVE multiplayer in this regard)
        /// and then move to the settings-lobby gamestate.
        /// </summary>
        private static IEnumerator<AsyncTaskStatus> AsyncTask_GotoSettingsLobbyLocal()
        {
            // disable the GUI while we are creating a session - from this point on player cannot quit this screen
            GUIManager.Instance.DeactivateGUI();

            // NOTE: no need to do the session creation async, but do need to wait for P2P to stop
            HighscoresP2P.Instance.Stop();

            while (!HighscoresP2P.Instance.IsStopped) yield return null;

            // double check all sessions stopped
            if (NetworkSessionManager.Instance.IsActiveSession)
            {
                NetworkSessionManager.Instance.ShutdownSession();
            }

            try
            {
                NetworkSession session = NetworkSession.Create(NetworkSessionType.Local, 2, 2);
                NetworkSessionManager.Instance.Session = session;
            }
            catch (Exception e)
            {
                Assert.Fatal(false, "Local multiplayer match - can't create local session: " + e.Message);
                NetworkSessionManager.Instance.ShutdownSession();   // make sure session shutdown - silliness check really
                GameStateManager.Instance.ClearAndLoad(GameStateNames.MAINMENU, null);
                yield break;
            }

            // create a gameplaysettings instance that will persist (thus remembering settings for when
            // the players return to the settings-lobby)
            Game.Instance.ActiveGameplaySettings = new GamePlaySettings(Game.Instance.LastLocalSettings);

            // let's all go to the lobby...
            GameStateManager.Instance.Push(GameStateNames.SETTINGS_MULTIPLAYER_LOCAL, null);
        }
#endif



        public static GUIActionDelegate LocalMatch { get { return _localmatch; } }
        public static GUIActionDelegate XboxLIVE { get { return _xboxlive; } }

        private static GUIActionDelegate _localmatch = new GUIActionDelegate(delegate(GUIComponent guiComponent, string paramString)
        {
            Debug.WriteLine("(Action) pushing gamestate Local Multiplayer Settings");

            _lastSelectedButton = GUIManager.Instance.GetFocused().Owner.Name;

#if XBOX
            Game.Instance.AddAsyncTask(AsyncTask_GotoSettingsLobbyLocal(), true);
#else
            // PC version can't do multiplayer stuff
            Debug.WriteLine("(Action) (PC) *not* pushing gamestate Local Multiplayer Settings");
#endif
        });

        private static GUIActionDelegate _xboxlive = new GUIActionDelegate(delegate(GUIComponent guiComponent, string paramString)
        {
            Debug.WriteLine("(Action) pushing gamestate Xbox LIVE");

            _lastSelectedButton = GUIManager.Instance.GetFocused().Owner.Name;

//#if XBOX
            GameStateManager.Instance.Push(GameStateNames.XBOXLIVE, null);
//#else
//            // PC version can't do multiplayer stuff
//            Debug.WriteLine("(Action) (PC) *not* pushing gamestate Xbox LIVE");
//#endif
        });
    }
}
