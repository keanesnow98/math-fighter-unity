using System;
using System.Collections.Generic;
////using System.Linq;
using System.Text;

using MathFreak.GUIFrameWork;
using GarageGames.Torque.Core;



namespace MathFreak.GameStateFramework
{
    /// <summary>
    /// Manages gamestates - push/pop/load gamestates - tick the active gamestate - coordinate
    /// transitioning between gamestates.
    /// </summary>
    class GameStateManager
    {
        private static GameStateManager _instance;

        private Dictionary<string, GameState> _gamestates;
        private Stack<string> _stack;
        private string[] _prefilledStackList;
        private string _current;

        private Dictionary<string, GameState> _backgroundGameStates;

        private GameState _transitioningOn;
        private GameState _transitioningOff;

        private bool _isTransitioningOn;
        private bool _isTransitioningOff;
        private bool _isPreTransitioningOff;
        private bool _isPreTransitioningOn;
        private String _paramString;

        private bool _clearAllGameStates;

        // returns the name of the current gamestate object
        public string CurrentName
        {
            get { return _current; }
        }

        // returns a reference to the actual gamestate object that is currently active
        public GameState CurrentObject
        {
            get { return (_current != null ? _gamestates[_current] : null); }
        }

        // returns the gamestate that is transitioning off if there is one.
        public GameState TransitioningOffState
        {
            get { return _transitioningOff; }
        }

        public static GameStateManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new GameStateManager();
                }

                return _instance;
            }
        }

        protected GameStateManager()
        {
            _gamestates = new Dictionary<string,GameState>();
            _stack = new Stack<string>();
            _backgroundGameStates = new Dictionary<string, GameState>();
        }

        public String GetNameOf(GameState state)
        {
            foreach (KeyValuePair<String, GameState> item in _gamestates)
            {
                if (item.Value == state) return item.Key;
            }
            
            // The following does not compile under .NET 2.0 - for PC we would be targetting .NET 2.0 for the smaller install size
            //int count = _gamestates.Count;

            //for (int i = 0; i < count; i++)
            //{
            //    if (_gamestates.ElementAt(i).Value == state) return _gamestates.ElementAt(i).Key;
            //}

            return null;
        }

        /// <summary>
        /// Returns the actual instance of a named gamestate.
        /// </summary>
        public GameState GetNamedState(String name)
        {
            GameState state = null;

            _gamestates.TryGetValue(name, out state);

            return state;
        }

        /// <summary>
        ///  Adds a game state to the internal list of avaliable states.
        /// </summary>
        /// <param name="name">name of the game state</param>
        /// <param name="state">reference to instance of gamestate to add</param>
        public void Add(string name, GameState state)
        {
            if (_gamestates.ContainsKey(name))
            {
                throw new Exception("GameStateManager::Add(" + name + ") - gamestate name already in use");
            }

            _gamestates.Add(name, state);
            state.Init();
        }

        /// <summary>
		/// Unloads the current gamestate and loads the new one.
		/// Does not alter the stack.
        /// </summary>
        /// <param name="name">name of the gamestate to load</param>
		public void Load(string name, string paramString)
		{
            Assert.Fatal(_transitioningOn == null, "Can't Load gamestate - already in the middle of transitioning another gamestate change (on:" + _transitioningOn + ") (off:" + _transitioningOff + ") (rejected:" + name + ")");
            Assert.Fatal(_transitioningOff == null, "Can't Load gamestate - already in the middle of transitioning another gamestate change (on:" + _transitioningOn + ") (off:" + _transitioningOff + ") (rejected:" + name + ")");

			// check if same as current state
			if (name != null && name == _current)
			{
                throw new Exception("GameStateManager::Load(" + name + ") - gamestate is already the current state");
			}

            // get refs to the old and new gamestates
            _transitioningOff = null;

            // if there is a current gamestate and it's not a background state then we'll need to transition it off
            if (_current != null && !_backgroundGameStates.ContainsKey(_current))
            {
                _transitioningOff = _gamestates[_current];
            }

            // if the gamestate we are loading is currently a background state then we *don't* need to transition it on
            if (_backgroundGameStates.ContainsKey(name))
            {
                _transitioningOn = null;
            }
            // else we *do* need to transition on the gamestate that is being loaded
            else if (!_gamestates.TryGetValue(name, out _transitioningOn))
            {
                throw new Exception("GameStateManager::Load(" + name + ") - gamestate does not exist");
            }

            if (_transitioningOff != null)
            {
                _isPreTransitioningOff = true;
            }

            if (_transitioningOn != null)
            {
                _isPreTransitioningOn = true;
            }

            _paramString = paramString;

            _current = name;
        }

        /// <summary>
        /// pushes the current gamestate on the stack and loads the new gamestate.
        /// </summary>
        /// <param name="name">name of the gamestate to make active</param>
        public void Push(string name, string paramString)
		{
			if (_current != null)
			{
                // check this state isn't already on the stack
                if (_stack.Contains(name))
                {
					throw new Exception("GameStateManager::Push(" + name + ") - gamestate is already on the stack");
				}

                _stack.Push(_current);
			}

            Load(name, paramString);
		}
		
        /// <summary>
        /// If the stack is not empty, unloads the current gamestate, pops the previous
        /// gamestate from the stack and loads that gamestate.
        /// </summary>
		public void Pop()
		{
            Pop(null);
		}

        public void Pop(string paramString)
        {
            if (_stack.Count != 0)
            {
                Load(_stack.Pop(), paramString);
            }
            else
            {
                throw new Exception("GameStateManager::Pop() - stack is empty");
            }
        }

        /// <summary>
        /// Same as for Pop(), but no 'transition off' gets triggered.
        /// If the gamestate that becomes the new active gamestate is
        /// a background state then it will be brought to the foreground
        /// and no 'transition on' gets triggered.
        /// </summary>
        public void PopImmediately(string paramString)
        {
            if (_stack.Count != 0 && _current != null)
            {
                // unload the current gamestate without ceremony
                _gamestates[_current].UnloadedImmediately();

                // pop the previous gamestate and make it the current gamestate
                string prevState = _stack.Pop();

                // if it's not a background gamestate then load it the normal way - thus triggering 'transition on'
                if (!_backgroundGameStates.ContainsKey(prevState))
                {
                    Load(prevState, paramString);
                }
                // else no need to 'transition on', so just make sure the GUI is actived and bring the state to the foreground
                else
                {
                    _current = prevState;
                    GUIManager.Instance.ActivateGUI();
                    SetCurrentAsBackground(false);
                }
            }
            else
            {
                throw new Exception("GameStateManager::PopImmediately() - stack is empty");
            }
        }

        public void PopOverlaysImmediately()
        {
            if (_backgroundGameStates.Count > 0)
            {
                // if we have any BG states then pop everything until we only have no BG states left
                while (_backgroundGameStates.Count > 0)
                {
                    _gamestates[_current].UnloadedImmediately();
                    _current = _stack.Pop();
                    _backgroundGameStates.Remove(_current);
                }

                // let the new current state know it has become the foreground state
                _gamestates[_current].OnSetAsForeground();
            }
        }
		
        /// <summary>
        /// Clears the stack, unloads the current gamestate and loads the new gamestate.
        /// </summary>
        /// <param name="name">name of the gamestate to load</param>
        public void ClearAndLoad(string name, string paramString)
		{
            PopOverlaysImmediately();

            // do the clear and load (or, in fact, the load and clear)
			Load(name, paramString);
            _clearAllGameStates = true;
		}

        /// <summary>
        /// Does the same as ClearAndLoad(), but after all the transitioning between gamestates
        /// has finished a list of gamestates will be 'pushed under' the active gamestate.
        /// </summary>
        public void ClearAndLoadWithPrefilledStack(string name, string[] prefilledStackList, string paramString)
        {
            _prefilledStackList = prefilledStackList;

            Load(name, paramString);
            _clearAllGameStates = true;
        }

        // Pushes a gamestate as an overlay - this means that the currently active gamestate will not
        // be removed.  In order to achieve this the active gamestate will be made a background
        // gamestate until the overlay disappears.
        public void PushOverlay(string name, string paramString)
        {
            SetCurrentAsBackground(true);
            Push(name, paramString);
        }

        // sets or unsets the current state as a background one
        public void SetCurrentAsBackground(bool setAsBackground)
        {
            // check there is actually a gamestate active
            Assert.Fatal(_current != null, "Can't set/unset gamestate as background as there is no current foreground gamestate");
            if (_current == null) return;

            if (setAsBackground)
            {
                Assert.Fatal(!_backgroundGameStates.ContainsKey(_current), "Setting current state as background - but already in the bg list");

                if (!_backgroundGameStates.ContainsKey(_current))
                {
                    _backgroundGameStates.Add(_current, _gamestates[_current]);
                }

                // let the gamestate know, incase it needs to do anything
                _gamestates[_current].OnSetAsBackground();
            }
            else
            {
                Assert.Fatal(_backgroundGameStates.ContainsKey(_current), "Un-Setting current state as background - but is not in the bg list");

                if (_backgroundGameStates.ContainsKey(_current))
                {
                    _backgroundGameStates.Remove(_current);
                }

                // let the gamestate know, incase it needs to do anything
                _gamestates[_current].OnSetAsForeground();
            }
        }

        public bool CanAcceptInvite()
        {
            // first check if gamestates are in the process of transitioning - can't go careering off to play a match in the middle of gamestate transitioning
            if (_isPreTransitioningOff || _isPreTransitioningOn || _isTransitioningOff || _isTransitioningOn)
            {
                return false;
            }

            // then ask all the active gamestates (current and BG ones) if they will accept the invite
            foreach (KeyValuePair<String, GameState> bgGameState in _backgroundGameStates)
            {
                if (!bgGameState.Value.CanAcceptInvite()) return false;
            }

            return _gamestates[_current].CanAcceptInvite();
        }

        public void OnInviteAccepted()
        {
            // tell all the active gamestates that an invite was accepted - i.e. we are about to go to a new gamestate
            foreach (KeyValuePair<String, GameState> bgGameState in _backgroundGameStates)
            {
                bgGameState.Value.OnInviteAccepted();
            }

            _gamestates[_current].OnInviteAccepted();
        }

        /// <summary>
        /// Ticks gamestate and gamestate transitions
        /// </summary>
        public void Tick(float dt)
        {
            // tick background gamestates
            try
            {
                foreach (KeyValuePair<String, GameState> bgGameState in _backgroundGameStates)
                {
                    bgGameState.Value.Tick(dt);
                }
            }
            catch
            {
                // do nothing... we just want to avoid crashing when bg gamestate list is modified in mid iteration
            }

            // tick transitions
            if (_isPreTransitioningOff)
            {
                _transitioningOff.PreTransitionOff();
                _isPreTransitioningOff = false;
                _isTransitioningOff = true;
            }

            if (_isPreTransitioningOn)
            {
                _transitioningOn.PreTransitionOn(_paramString);
                _isPreTransitioningOn = false;
                _isTransitioningOn = true;
            }

            if (_isTransitioningOff || _isTransitioningOn)
            {
                if (_isTransitioningOff)
                {
                    if (_transitioningOff.TickTransitionOff(dt))
                    {
                        // completed the transition
                        _isTransitioningOff = false;
                    }
                }

                if (_isTransitioningOn)
                {
                    if (_transitioningOn.TickTransitionOn(dt, !_isTransitioningOff))    // passing whether the prev state has transitioned off yet as often we want the option to not overlap the transitions - the state that is transitioning on can check this bool var and decide when it is time to transition on
                    {
                        // completed the transition
                        _isTransitioningOn = false;
                    }
                }

                // check if both transitions have finished
                if (!_isTransitioningOff && !_isTransitioningOn)
                {
                    if (_transitioningOff != null)
                    {
                        _transitioningOff.OnTransitionOffCompleted();
                    }

                    if (_transitioningOn != null)
                    {
                        _transitioningOn.OnTransitionOnCompleted();
                    }

                    // if the newly active gamestate is in the background list then remove it from the
                    // background list reactivate it's GUI
                    if (_backgroundGameStates.ContainsKey(_current))
                    {
                        // reactivate the state's GUI and bring the state to the foreground again
                        GUIManager.Instance.ActivateGUI();
                        SetCurrentAsBackground(false);
                    }

                    _transitioningOff = null;
                    _transitioningOn = null;

                    if (_clearAllGameStates)
                    {
                        _clearAllGameStates = false;

                        GUIManager.Instance.SaveGUI();

                        // unload all the background gamestates
                        foreach (KeyValuePair<String, GameState> bgGameState in _backgroundGameStates)
                        {
                            bgGameState.Value.UnloadedImmediately();
                        }

                        _backgroundGameStates.Clear();

                        // clear the stack
                        _stack.Clear();

                        GUIManager.Instance.RestoreGUI();

                        // if there are gamestates to 'push under' the current gamestate then do that now
                        if (_prefilledStackList != null)
                        {
                            foreach (string name in _prefilledStackList)
                            {
                                _stack.Push(name);
                            }

                            _prefilledStackList = null;
                        }
                    }
                }
            }
            // tick active gamestate if it's not a background state (if it is a bg state then it will already have been ticked)
            else if (_current != null && !_backgroundGameStates.ContainsKey(_current))
            {
                _gamestates[_current].Tick(dt);
            }
        }
	}
}