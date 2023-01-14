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
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Net;
using System.Diagnostics;
using MathFreak.AsyncTaskFramework;
using Microsoft.Xna.Framework.Graphics;
using MathFreak.Text;
using MathFreak.GameStates.Dialogs;



namespace MathFreak.GameStates
{
    /// <summary>
    /// This gamestate handles allowing the player to choose an Xbox LIVE multiplayer
    /// match to join.
    /// </summary>
    [TorqueXmlSchemaType]
    public class GameStateChooseMatch : GameState
    {
        private static AvailableNetworkSessionCollection _availableSessions;
        private static bool _isSearchingForSessions;
        private static bool _isJoining;

        private static GameStateChooseMatch _this;
        private bool _isExiting;

        private const float UPDATE_DELAY = 1.0f;    // time between attempts to auto-update the session display (will not reget the session list, but will update session info such as number of players and session properties)
        private float _updateTimer;

        private const int PAGESIZE = 4;
        private int _startPos;
        private int _highlightedPos;
        private bool _invalidatedDisplay;

        
        public override void Init()
        {
            base.Init();
            _this = this;
        }

        public override void PreTransitionOn(string paramString)
        {
            base.PreTransitionOn(paramString);

            _availableSessions = null;
            _isJoining = false;
            _updateTimer = 0.0f;
            _startPos = 0;
            _highlightedPos = 0;
            _isExiting = false;

            Game.Instance.LoadScene(@"data\levels\ChooseMatch.txscene");

            // hide progress anim
            TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("choosematch_progressanim").Visible = false;

            // hide all the 'available session' display stuff initially
            TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("choosematch_nomatchesfound").Visible = false;

            for (int i = 0; i < PAGESIZE; i++)
            {
                TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("choosematch_button" + i).Visible = false;
                TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("choosematch_gamertag" + i).Visible = false;
                TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("choosematch_ping" + i).Visible = false;
            }

            for (int grade = 1; grade < 14; grade++)
            {
                TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("choosematch_grade" + grade).Visible = false;
            }

            T2DStaticSprite shadow = TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("choosematch_grade_shadow");
            shadow.Material.IsTranslucent = true;
            (shadow.Material as SimpleMaterial).IsColorBlended = true;
            shadow.VisibilityLevel = 0.5f;
            shadow.Visible = false;
        }

        public override void OnTransitionOnCompleted()
        {
            base.OnTransitionOnCompleted();

//#if XBOX
            // get the list of available sessions and set up the display
            Game.Instance.AddAsyncTask(AsyncTask_GetAndDisplayAvailableSessions(), true);
//#endif
        }

        public override void PreTransitionOff()
        {
            base.OnTransitionOffCompleted();
            Game.Instance.UnloadScene(@"data\levels\ChooseMatch.txscene");
        }

        public override void Tick(float dt)
        {
            base.Tick(dt);

            _updateTimer += dt;

            if (_updateTimer >= UPDATE_DELAY)
            {
                _updateTimer = 0.0f;
                _invalidatedDisplay = true;
            }

            if (_invalidatedDisplay)
            {
                _invalidatedDisplay = false;
                UpdateAvailableSessionsDisplay();
            }
        }

        /// <summary>
        /// Called to set up the scene to display the available sessions starting from the one
        /// specified.
        /// </summary>
        private void UpdateAvailableSessionsDisplay()
        {
            // if joining a session then don't update the sessions display - player already chose one (and also if we access the available sessions list it will be invalid anyway once joining begins)
            if (_isJoining) return;

            // first hide all the stuff (don't assume all the rows will be needed)
            for (int i = 0; i < PAGESIZE; i++)
            {
                TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("choosematch_button" + i).Visible = false;
                TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("choosematch_gamertag" + i).Visible = false;
                TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("choosematch_ping" + i).Visible = false;
            }

            for (int grade = 1; grade < 14; grade++)
            {
                TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("choosematch_grade" + grade).Visible = false;
            }

            TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("choosematch_grade_shadow").Visible = false;

            // if still searching for sessions then don't display anything
            if (_isSearchingForSessions) return;

            // if no sessions then show the 'no sessions message'
            if (_availableSessions == null || _availableSessions.Count == 0)
            {
                TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("choosematch_nomatchesfound").Visible = true;
                return;
            }
            // else hide that message
            else
            {
                TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("choosematch_nomatchesfound").Visible = false;
            }

            // for each row set the objects' properties to the appropriate stuff and make them visible
            for (int i = 0; i < PAGESIZE; i++)
            {
                int sessionIndex = _startPos + i;

                // check we haven't run out of sessions to display
                if (sessionIndex >= _availableSessions.Count) break;

                // get the session
                AvailableNetworkSession availableSession = _availableSessions[sessionIndex];

                // set up the objects and make them visible
                T2DStaticSprite obj = TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("choosematch_button" + i);
                obj.Visible = true;
                obj.Components.FindComponent<MFAvailableMatchHighlighter>().SessionIndex = sessionIndex;

                obj = TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("choosematch_gamertag" + i);
                obj.Visible = true;
                obj.Components.FindComponent<MathMultiTextComponent>().TextValue = _availableSessions[sessionIndex].HostGamertag;

                obj = TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("choosematch_ping" + i);
                obj.Visible = true;
                obj.Material = TorqueObjectDatabase.Instance.FindObject<RenderMaterial>("Ping_Bars2Material");
                int ping = (_availableSessions[i].QualityOfService.AverageRoundtripTime.Milliseconds) / 100; // arbitrarily rate ping of < 100ms as > 500ms as worst - TODO: change this if other values required in practice
                obj.MaterialRegionIndex = (ping < 6 ? ping : 5);
            }

            if (_highlightedPos >= _availableSessions.Count)
            {
                _highlightedPos = _availableSessions.Count - 1;
            }

            if (_highlightedPos != -1)
            {
                MFAvailableMatchHighlighter highlighter = TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("choosematch_button" + (_highlightedPos - _startPos)).Components.FindComponent<MFAvailableMatchHighlighter>();
                GUIManager.Instance.SetFocus(highlighter);

                int grades = (int)(_availableSessions[_highlightedPos + _startPos].SessionProperties[(int)NetworkSessionManager.EnumSessionProperty.Grades]);

                for (int grade = 1; grade < 14; grade++)
                {
                    T2DSceneObject obj = TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("choosematch_grade" + grade);
                    obj.Visible = true;
                    obj.Components.FindComponent<MFGradeCheckboxButton>().IsChecked = GamePlaySettings.MathGradeIsEnabled(grades, grade);
                }

                TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("choosematch_grade_shadow").Visible = false;
            }
        }

//#if XBOX
        /// <summary>
        /// This task gets a list of available matches and sets up the scene to list them
        /// </summary>
        private IEnumerator<AsyncTaskStatus> AsyncTask_GetAndDisplayAvailableSessions()
        {
            if (_isExiting) yield break;

            TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("choosematch_progressanim").Components.FindComponent<LoadSearchComponent>().SetMessage("Searching");

            _isSearchingForSessions = true;

            // show progress anim
            yield return null;  // pause for one tick so the text can update (note: also needed to make sure not still technically transitioning on if we happen to need to immediately show a notification dialog)
            TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("choosematch_progressanim").Visible = true;

            // Start the async session finding
            IAsyncResult result;

            try
            {
                result = NetworkSessionManager.Instance.BeginFind(NetworkSessionManager.PLAYERMATCH, 1, GamePlaySettings.GetSessionPropertiesPattern(), null, null);
            }
            catch (Exception e) // generic exception catching as we just want to abort if anything goes wrong
            {
                // something went really wrong - abort!
                Assert.Warn(false, "Warning: Could not find any LIVE multiplayer matches - BeginFind() threw an exception:\n" + e.Message);
                NetworkSessionManager.Instance.ShutdownSession();   // incase the error was due to already having a session active
                TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("choosematch_progressanim").Visible = false;
                _availableSessions = null;
                _isSearchingForSessions = false;
                UpdateAvailableSessionsDisplay();
                GUIManager.Instance.ActivateGUI();  // enable the GUI
                yield break;
            }

            // Wait until the find operation has completed
            while (!result.IsCompleted && !_isExiting) yield return null;

            if (_isExiting)
            {
                NetworkSessionManager.Instance.ShutdownSession();
                TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("choosematch_progressanim").Visible = false;
                yield break;
            }

            // get the list of available sessions
            try
            {
                _availableSessions = NetworkSessionManager.Instance.EndFind(result);
            }
            catch (Exception e) // EndFind() is not documented as throwing any exceptions but it does!!!
            {
                Assert.Warn(false, "Warning: could not get available sessions list - EndFind() threw an exception:\n" + e.Message);
                NetworkSessionManager.Instance.ShutdownSession();   // incase the error was due to already having a session active
                _availableSessions = null;
            }

            // hide progress anim
            TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("choosematch_progressanim").Visible = false;

            // show the available sessions starting at the first one
            _isSearchingForSessions = false;
            UpdateAvailableSessionsDisplay();

            // enable the GUI
            GUIManager.Instance.ActivateGUI();
        }

        /// <summary>
        /// This task will join a session
        /// </summary>
        private IEnumerator<AsyncTaskStatus> AsyncTask_JoinSession(int sessionIndex)
        {
            TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("choosematch_progressanim").Components.FindComponent<LoadSearchComponent>().SetMessage("Joining");

            _isJoining = true;

            // show progress anim
            yield return null;  // pause for one tick so the text can update
            TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("choosematch_progressanim").Visible = true;

            // join the LIVE session
            IAsyncResult result;

            try
            {
                result = NetworkSessionManager.Instance.BeginJoin(_availableSessions[sessionIndex], null, null);
            }
            catch (Exception e) // generic exception catching as we just want to abort if anything goes wrong
            {
                // something went really wrong - abort!
                _isExiting = true;
                Assert.Warn(false, "Warning: Could not join LIVE multiplayer match - BeginJoin() threw an exception:\n" + e.Message);
                NetworkSessionManager.Instance.ShutdownSession();   // incase the error was due to already having a session active
                TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("choosematch_progressanim").Visible = false;
                DialogNotification.Show(DialogNotification.MESSAGE_COULDNOTJOINMATCH, _quitChooseMatchScreen);
                yield break;
            }

            // Wait until the joining process completes
            while (!result.IsCompleted && !_isExiting) yield return null;

            if (_isExiting)
            {
                TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("choosematch_progressanim").Visible = false;
                NetworkSessionManager.Instance.ShutdownSession();
                TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("choosematch_progressanim").Visible = false;
                yield break;
            }

            try
            {
                // get the session we we have joined
                NetworkSession session = NetworkSessionManager.Instance.EndJoin(result);
                NetworkSessionManager.Instance.Session = session;
            }
            catch (Exception e)
            {
                // we couldn't join the session
                _isExiting = true;
                NetworkSessionManager.Instance.ShutdownSession();
                Assert.Warn(false, "Warning: Could not join LIVE multiplayer match - EndJoin() threw an exception:\n" + e.Message);
                TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("choosematch_progressanim").Visible = false;
                DialogNotification.Show(DialogNotification.MESSAGE_COULDNOTJOINMATCH, _quitChooseMatchScreen);
                yield break;
            }

            // create a game settings object for the gameplay to use
            Game.Instance.ActiveGameplaySettings = new GamePlaySettingsMultiplayerLIVE();

            // wait for the host to tell us which gamestate to goto
            LocalNetworkGamer localGamer = NetworkSessionManager.Instance.Session.LocalGamers[0];
            NetworkMessage msg = new NetworkMessage();

            while (msg.Type != NetworkMessage.EnumType.GameStateJoined && !_isExiting && NetworkSessionManager.Instance.IsValidSession)
            {
                if (localGamer.IsDataAvailable)
                {
                    NetworkSessionManager.Instance.RecieveDataFromHost(msg, localGamer);
                }

                yield return null;
            }

            if (_isExiting)
            {
                TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("choosematch_progressanim").Visible = false;
                NetworkSessionManager.Instance.ShutdownSession();
                TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("choosematch_progressanim").Visible = false;
                yield break;
            }

            if (!NetworkSessionManager.Instance.IsValidSession)
            {
                _isExiting = true;
                TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("choosematch_progressanim").Visible = false;
                NetworkSessionManager.Instance.ShutdownSession();
                TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("choosematch_progressanim").Visible = false;
                DialogNotification.Show(DialogNotification.MESSAGE_COULDNOTJOINMATCH, _quitChooseMatchScreen);
                yield break;
            }

            // hide progress anim
            TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("choosematch_progressanim").Visible = false;

            // goto the gamestate that the game is currently in
            GameStateManager.Instance.Push(msg.Data as string, GameStateSettingsMultiPlayerLIVE.ISJOINING);
        }
//#endif

        private void SetStartPos(int pos)
        {
            int count = 27;//_availableSessions.Count;

            // check for going too far down the list
            if (pos > count - PAGESIZE)
            {
                pos = count - PAGESIZE;
            }

            // check for going too far up the list
            if (pos < 0)
            {
                pos = 0;
            }

            // if position has changed then update
            if (pos != _startPos)
            {
                _startPos = pos;
                _invalidatedDisplay = true;
            }
        }

        private void OnAction_Back()
        {
            if (!_isExiting)
            {
                _isExiting = true;

                GameStateManager.Instance.Pop();
                //GameStateManager.Instance.ClearAndLoad(GameStateNames.MAINMENU, null);
            }
        }

        private void OnAction_Play(int sessionIndex)
        {
            if (_isExiting || _availableSessions.Count == 0) return;

            Game.Instance.AddAsyncTask(_this.AsyncTask_JoinSession(sessionIndex), false);
        }

        private void OnAction_Up()
        {
            if (_isExiting || _availableSessions.Count == 0) return;

            int newHighlightedPos = _highlightedPos - 1;

            // scrolling up?
            if (newHighlightedPos < _startPos)
            {
                SetStartPos(_startPos - 1);
                newHighlightedPos = _startPos;
                _highlightedPos = newHighlightedPos;
            }

            // check if highlighter moved
            if (newHighlightedPos != _highlightedPos)
            {
                _highlightedPos = newHighlightedPos;
                _invalidatedDisplay = true;
            }
        }

        private void OnAction_Down()
        {
            if (_isExiting || _availableSessions.Count == 0) return;

            int newHighlightedPos = _highlightedPos + 1;

            // scrolling down?  (note: will automatically trigger an update of the display if scrolling occurs)
            if (newHighlightedPos >= _startPos + PAGESIZE)
            {
                SetStartPos(_startPos + 1);
                _highlightedPos = _startPos + PAGESIZE - 1;
            }

            // check if highlighter moved
            if (newHighlightedPos != _highlightedPos)
            {
                _highlightedPos = newHighlightedPos;
                _invalidatedDisplay = true;
            }
        }

        private void OnAction_PageUp()
        {
            if (_isExiting || _availableSessions.Count == 0) return;

            SetStartPos(_startPos - PAGESIZE);

            if (_startPos != _highlightedPos)
            {
                _highlightedPos = _startPos;
                _invalidatedDisplay = true;
            }
        }

        private void OnAction_PageDown()
        {
            if (_isExiting || _availableSessions.Count == 0) return;

            SetStartPos(_startPos + PAGESIZE);

            if (_startPos + PAGESIZE - 1 != _highlightedPos)
            {
                _highlightedPos = _startPos + PAGESIZE - 1;
                _invalidatedDisplay = true;
            }
        }



        public static GUIActionDelegate Back { get { return _back; } }
        public static GUIActionDelegate Play { get { return _play; } }
        public static GUIActionDelegate PageUp { get { return _pageUp; } }
        public static GUIActionDelegate PageDown { get { return _pageDown; } }
        public static GUIActionDelegate Up { get { return _up; } }
        public static GUIActionDelegate Down { get { return _down; } }

        private static GUIActionDelegate _back = new GUIActionDelegate(delegate(GUIComponent guiComponent, string paramString)
        {
            Debug.WriteLine("(Action) quitting choose-match and going to main menu (if not already exiting the gamestate)");
            _this.OnAction_Back();
        });

        private static GUIActionDelegate _play = new GUIActionDelegate(delegate(GUIComponent guiComponent, string paramString)
        {
            Debug.WriteLine("(Action) joining a match???");
            _this.OnAction_Play((guiComponent as MFAvailableMatchHighlighter).SessionIndex);
        });

        private static GUIActionDelegate _pageUp = new GUIActionDelegate(delegate(GUIComponent guiComponent, string paramString)
        {
            _this.OnAction_PageUp();
        });

        private static GUIActionDelegate _pageDown = new GUIActionDelegate(delegate(GUIComponent guiComponent, string paramString)
        {
            _this.OnAction_PageDown();
        });

        private static GUIActionDelegate _up = new GUIActionDelegate(delegate(GUIComponent guiComponent, string paramString)
        {
            _this.OnAction_Up();
        });

        private static GUIActionDelegate _down = new GUIActionDelegate(delegate(GUIComponent guiComponent, string paramString)
        {
            _this.OnAction_Down();
        });


        private static GUIActionDelegate _quitChooseMatchScreen = new GUIActionDelegate(delegate(GUIComponent guiComponent, string paramString)
        {
            Debug.WriteLine("(Action) quitting the choose match screen");
            GameStateManager.Instance.ClearAndLoad(GameStateNames.MAINMENU, null);
        });
    }
}
