using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

using GarageGames.Torque.T2D;
using GarageGames.Torque.Core;
using GarageGames.Torque.Sim;



namespace MathFreak.GUIFrameWork
{
    /// <summary>
    /// Manages the GUI stuff (well, duh! ;-)
    /// In particular, it tracks the focused GUI component and routes input (from keyboard/gamepad).
    /// Also manages a GUI stack - multiple groups of GUI components (of which only one group can be active
    /// at any one time).
    /// 
    /// NOTE: we aren't using the TX GUI stuff because that stuff sucks in many ways :)
    /// </summary>
    public class GUIManager
    {
        private static GUIManager _instance;

        // to stop multiple GUI actions getting triggered at the same time (e.g. if the user clicks
        // with mouse AND keyboard simultaneously), we keep track of whether any action is already being
        // processed and don't execute any more actions until the processing status is IDLE again.
        // (note: the inclusion of the completed state ensures we have a single frame delay before the next
        // action can be executed - the completed state effectively delays the return to the idle state by one frame -
        // which is especially important for the many actions that will report as completed immediately after
        // being executed).
        private enum EnumActionProcessingStatus { Idle, Completed, Processing };
        private EnumActionProcessingStatus _actionProcessingStatus;

        private Stack<GUIComponentGroup> _GUIgroups;
        private GUIComponentGroup _currentGUI;
        private GUIComponentGroup _savedGUI;

        // restrain the rate at which the movement inputs can be received or the gui will be unusably responsive
        private const float INITIAL_DELAY = 0.5f;   // delay before the first repeat of same movement
        private const float REPEAT_DELAY = 0.12f;    // delay between repeats of the same movement
        private float _delayRemaining = 0.0f;

        private enum EnumMoveType { None, North, South, East, West };
        private EnumMoveType _prevMoveType;

        public static GUIManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new GUIManager();
                }

                return _instance;
            }
        }

        private GUIManager()
        {
            _GUIgroups = new Stack<GUIComponentGroup>();
            _currentGUI = new GUIComponentGroup();
            Reset();
        }

        public void Reset()
        {
            _actionProcessingStatus = EnumActionProcessingStatus.Idle;
            _prevMoveType = EnumMoveType.None;
        }

        /// <summary>
        /// Pushes the current gui and prepares a new gui group for active duty
        /// </summary>
        public void PushGUI()
        {
            _currentGUI.OnDeactivated();
            _GUIgroups.Push(_currentGUI);
            _currentGUI = new GUIComponentGroup();
            //_currentGUI.OnActivated();
            Reset();
        }

        /// <summary>
        /// Discards the current gui, pops the most recent previous gui off the stack and makes it the active one
        /// </summary>
        public void PopGUI()
        {
            if (_GUIgroups.Count > 0)
            {
                _currentGUI.OnDeactivated();
                _currentGUI = _GUIgroups.Pop();
                //_currentGUI.OnActivated();

                //if (_currentGUI._inputSource != null)
                //{
                //    _currentGUI._inputSource.EnableInput();
                //}
            }
        }

        /// <summary>
        /// Pops the current GUI off, but saves it on one side for later (NOTE: there can only be one saved GUI at any one time).
        /// This GUI can be later restored. (used by gamestatemanager when popping GUI's of gamestates that are 'under' another gamestate
        /// on the stack.
        /// </summary>
        public void SaveGUI()
        {
            if (_GUIgroups.Count > 0)
            {
                Debug.WriteLine("Saving GUI");
                _savedGUI = _currentGUI;
                PopGUI();
            }
            else
            {
                _savedGUI = null;
            }
        }

        /// <summary>
        /// Restores previously 'saved' GUI - see SaveGUI() for more details
        /// </summary>
        public void RestoreGUI()
        {
            if (_savedGUI != null)
            {
                Debug.WriteLine("Restoring GUI");

                _currentGUI.OnDeactivated();
                _GUIgroups.Push(_currentGUI);
                _currentGUI = _savedGUI;
                ActivateGUI();
                Reset();
            }
        }

        // used to prevent the GUI responding to the click button until it has been released and pressed again - can be needed on rare occassions
        public void SetClickButtonAsDown()
        {
            _currentGUI.SetClickButtonAsDown();
        }
        public void SetInputSource(GUIInputComponent source)
        {
            _currentGUI._inputSource = source;
        }

        public void SetDefault(GUIComponent comp)
        {
            _currentGUI.SetDefault(comp);
        }

        public GUIComponent GetFocused()
        {
            return _currentGUI.GetFocused();
        }

        public void SetFocus(GUIComponent comp)
        {
            _currentGUI.SetFocus(comp);
        }

        public void ActivateGUI()
        {
            if (_currentGUI != null)
            {
                _currentGUI.OnActivated();
            }
        }

        public void DeactivateGUI()
        {
            if (_currentGUI != null)
            {
                _currentGUI.OnDeactivated();
            }
        }

        public void AddNonNavigableGUIComponent(GUIComponent comp)
        {
            _currentGUI.AddNonNavigableGUIComponent(comp);
        }

        public void RemoveNonNavigableGUIComponent(GUIComponent comp)
        {
            _currentGUI.RemoveNonNavigableGUIComponent(comp);
        }

        public void AddNavigableGUIComponent(GUIComponent comp)
        {
            _currentGUI.AddNavigableGUIComponent(comp);
        }

        public void RemoveNavigableGUIComponent(GUIComponent comp)
        {
            _currentGUI.RemoveNavigableGUIComponent(comp);
        }

        public void OnActionStarted()
        {
            _actionProcessingStatus = EnumActionProcessingStatus.Processing;
        }

        public void OnActionCompleted()
        {
            _actionProcessingStatus = EnumActionProcessingStatus.Completed;
        }

        /// <summary>
        /// Called before any game ticks are called - allows the manager to get ready for the
        /// next frame of GUI processing.
        /// </summary>
        public void PreTick()
        {
            if (_actionProcessingStatus == EnumActionProcessingStatus.Completed)
            {
                _actionProcessingStatus = EnumActionProcessingStatus.Idle;
            }
        }

        /// <summary>
        /// Process gamepad/keyboard input.  See the OnMouseXXXX() methods for processing mouse input.
        /// </summary>
        /// <param name="move"></param>
        /// <param name="dt"></param>
        public void ProcessInput(Move move, float dt)
        {
            if (_currentGUI == null) return;

            _currentGUI.UpdatePreviousButtonValue(move);

            if (_actionProcessingStatus != EnumActionProcessingStatus.Idle) return;

            _currentGUI.ProcessNonNavigableComponents(move, dt);

            if (_actionProcessingStatus != EnumActionProcessingStatus.Idle) return;

            _currentGUI.ProcessFocusedComponent(move, dt);

            if (_actionProcessingStatus != EnumActionProcessingStatus.Idle) return;

            // now process navigation - this takes a bit more work as we need to apply repeat delay stuff

            // first we'll test what the current input will do movement-wise if we actually processed it for real
            EnumMoveType moveType = _currentGUI.TestNavigation(move, dt);

            // if there was movement then we'll need to decide what to do about it and this will depend upon whether we need to delay stuff or not
            if (moveType != EnumMoveType.None)
            {
                // if the movement is different from the previously recorded movement then no need to delay
                if (moveType != _prevMoveType)
                {
                    _prevMoveType = moveType;
                    _delayRemaining = INITIAL_DELAY; // any further repetition of this move type will be delayed by this amount
                    _currentGUI.ProcessNavigation(move, dt);    // do it
                    return;
                }

                // else we need to decide what to do about delaying the repetition

                // first handle any already active delay
                _delayRemaining -= dt;

                if (_delayRemaining > 0.0f)
                {
                    return; // not finished delaying yet
                }
                else
                {
                    _delayRemaining = 0.0f;
                }

                // do the move
                _currentGUI.ProcessNavigation(move, dt);

                // setup the next delay period
                _delayRemaining = REPEAT_DELAY;
                return;
            }
            
            // else give the focused component a chance to process the move keys itself (e.g. for
            // an audio volume control left/right may alter the volume)
            if (_currentGUI.GetFocused() != null)
            {
                if (move.Sticks[0].X < -0.1)
                {
                    moveType = EnumMoveType.West;
                }
                else if (move.Sticks[0].X > 0.1)
                {
                    moveType = EnumMoveType.East;
                }
                else if (move.Sticks[0].Y > 0.1)
                {
                    moveType = EnumMoveType.North;
                }
                else if (move.Sticks[0].Y < -0.1)
                {
                    moveType = EnumMoveType.South;
                }

                // if there was movement then we'll need to decide what to do about it and this will depend upon whether we need to delay stuff or not
                if (moveType != EnumMoveType.None)
                {
                    // if the movement is different from the previously recorded movement then no need to delay
                    if (moveType != _prevMoveType)
                    {
                        _prevMoveType = moveType;
                        _delayRemaining = INITIAL_DELAY; // any further repetition of this move type will be delayed by this amount
                        _currentGUI.ProcessFocusedComponentNonNavMovement(moveType, dt);    // do it
                        return;
                    }

                    // else we need to decide what to do about delaying the repetition

                    // first handle any already active delay
                    _delayRemaining -= dt;

                    if (_delayRemaining > 0.0f)
                    {
                        return; // not finished delaying yet
                    }
                    else
                    {
                        _delayRemaining = 0.0f;
                    }

                    // do the move
                    _currentGUI.ProcessFocusedComponentNonNavMovement(moveType, dt);    // do it

                    // setup the next delay period
                    _delayRemaining = REPEAT_DELAY;
                    return;
                }
            }

            // else the movement input has stopped and we can reset delays and stuff (if the player wants to repeateadly press/release the buttons/sticks/keys really fast then that's fine; we won't stop them doing that and having the GUI respond to it)
            _prevMoveType = EnumMoveType.None;
            _delayRemaining = 0.0f;
        }


        
        private class GUIComponentGroup
        {
            public GUIInputComponent _inputSource;

            private List<GUIComponent> _nonNavigableComponents;
            private List<GUIComponent> _navigableComponents;
            private GUIComponent _focusedComponent;

            private GUIComponent _default;

            private int _prevButton = -1;

            public GUIComponentGroup()
            {
                _nonNavigableComponents = new List<GUIComponent>();
                _navigableComponents = new List<GUIComponent>();
            }

            public void SetDefault(GUIComponent comp)
            {
                _default = comp;
            }

            public GUIComponent GetFocused()
            {
                return _focusedComponent;
            }

            public void SetFocus(GUIComponent comp)
            {
                if (_focusedComponent == comp) return;

                if (_focusedComponent != null)
                {
                    _focusedComponent.OnLostFocus();
                }

                _focusedComponent = comp;

                if (_focusedComponent != null)
                {
                    _focusedComponent.OnGainedFocus();
                }
            }

            public void AddNonNavigableGUIComponent(GUIComponent comp)
            {
                _nonNavigableComponents.Add(comp);
            }

            public void RemoveNonNavigableGUIComponent(GUIComponent comp)
            {
                _nonNavigableComponents.Remove(comp);
            }

            public void AddNavigableGUIComponent(GUIComponent comp)
            {
                _navigableComponents.Add(comp);
            }

            public void RemoveNavigableGUIComponent(GUIComponent comp)
            {
                if (comp == _focusedComponent)
                {
                    _focusedComponent = null;
                }

                if (comp == _default)
                {
                    _default = null;
                }

                _navigableComponents.Remove(comp);
            }

            /// <summary>
            /// Called when this gui group becomes the active one
            /// </summary>
            public void OnActivated()
            {
                if (_inputSource != null)
                {
                    _inputSource.EnableInput();
                }

                if (_focusedComponent == null && _default != null)
                {
                    _focusedComponent = _default;
                    _focusedComponent.OnGainedFocus();
                }
            }

            /// <summary>
            /// Called when this gui group becomes inactive.  This doesn't mean the gui group has
            /// been permanently removed from play - just that it isn't active right now (maybe there
            /// is a popup dialog gui taking input right now and control will return to us later)
            /// </summary>
            public void OnDeactivated()
            {
                if (_inputSource != null)
                {
                    _inputSource.DisableInput();
                }
            }

            public void ProcessNonNavigableComponents(Move move, float dt)
            {
                foreach (GUIComponent comp in _nonNavigableComponents)
                {
                    int button = comp.OnProcessButtons(move.Buttons, _prevButton);

                    if (button != -1)
                    {
                        _prevButton = button;
                        break;
                    }
                }
            }

            /// <summary>
            /// Process stuff for the currently focused component
            /// </summary>
            public void ProcessFocusedComponent(Move move, float dt)
            {
                if (_focusedComponent == null) return;

                if (move.Buttons[GUIInputComponent.BUTTON_A].Pushed)
                {
                    if (_prevButton != GUIInputComponent.BUTTON_A)
                    {
                        _prevButton = GUIInputComponent.BUTTON_A;
                        _focusedComponent.OnClicked();
                    }
                }
                else
                {
                    int button = _focusedComponent.OnProcessButtons(move.Buttons, _prevButton);

                    if (button != -1)
                    {
                        _prevButton = button;
                    }
                }
            }

            /// <summary>
            /// Process navigation between components
            /// </summary>
            public void ProcessNavigation(Move move, float dt)
            {
                if (_focusedComponent == null) return;

                //Debug.WriteLine("reached process nav");

                T2DSceneObject obj = _focusedComponent.NextEast;
                if (move.Sticks[0].X > 0.1f && obj != null)
                {
                    SetFocus(obj.Components.FindComponent<GUIComponent>());
                    return;
                }

                obj = _focusedComponent.NextWest;
                if (move.Sticks[0].X < -0.1f && obj != null)
                {
                    SetFocus(obj.Components.FindComponent<GUIComponent>());
                    return;
                }

                obj = _focusedComponent.NextSouth;
                if (move.Sticks[0].Y < -0.1f && obj != null)
                {
                    SetFocus(obj.Components.FindComponent<GUIComponent>());
                    return;
                }

                obj = _focusedComponent.NextNorth;
                if (move.Sticks[0].Y > 0.1f && obj != null)
                {
                    SetFocus(obj.Components.FindComponent<GUIComponent>());
                    return;
                }

                return;
            }

            // This is for processing movement inputs on the currently focused gui component.
            // The inputs processed do NOT cause navigation between components - rather
            // it is the opportunity for a component to do something based on what the
            // input is (such as left/right being used to modify audio volume).
            public void ProcessFocusedComponentNonNavMovement(EnumMoveType moveType, float dt)
            {
                if (_focusedComponent == null) return;

                switch (moveType)
                {
                    case EnumMoveType.East:
                        _focusedComponent.OnMoveRight();
                        break;

                    case EnumMoveType.West:
                        _focusedComponent.OnMoveLeft();
                        break;

                    case EnumMoveType.North:
                        _focusedComponent.OnMoveUp();
                        break;

                    case EnumMoveType.South:
                        _focusedComponent.OnMoveDown();
                        break;

                    default:
                        Assert.Fatal(false, "Can only process north/south/east/west non-navigation movement");
                        break;
                }
            }

            /// <summary>
            /// Called to test what movement will happen if the specified movement is processed
            /// </summary>
            public EnumMoveType TestNavigation(Move move, float dt)
            {
                if (_focusedComponent == null) return EnumMoveType.None;

                if (move.Sticks[0].X > 0.1f && _focusedComponent.NextEast != null) return EnumMoveType.East;
                if (move.Sticks[0].X < -0.1f && _focusedComponent.NextWest != null) return EnumMoveType.West;
                if (move.Sticks[0].Y < -0.1f && _focusedComponent.NextSouth != null) return EnumMoveType.South;
                if (move.Sticks[0].Y > 0.1f && _focusedComponent.NextNorth != null) return EnumMoveType.North;

                return EnumMoveType.None;
            }

            public void SetClickButtonAsDown()
            {
                _prevButton = GUIInputComponent.BUTTON_A;
            }

            public void UpdatePreviousButtonValue(Move move)
            {
                if (_prevButton != -1 && !move.Buttons[_prevButton].Pushed)
                {
                    _prevButton = -1;
                }
            }
        }
    }
}
