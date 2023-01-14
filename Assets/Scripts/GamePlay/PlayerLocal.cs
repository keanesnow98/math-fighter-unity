using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Microsoft.Xna.Framework.GamerServices;
using GarageGames.Torque.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Net;
using GarageGames.Torque.T2D;
using GarageGames.Torque.Sim;
using GarageGames.Torque.Platform;
using Microsoft.Xna.Framework.Input;

namespace MathFreak.GamePlay
{
    /// <summary>
    /// Handles the local player specific stuff for the player representation
    /// 
    /// NOTE: different versions for XBOX and PC because we can't use the Gamer
    /// stuff on PC, but we still need to be able to test the single player game on PC.
    /// </summary>
    public class PlayerLocal : Player
    {
        private int _playerIndex;

        public int PlayerIndex
        {
            get { return _playerIndex; }
        }

//#if XBOX
        private Gamer _gamer;

        public override Gamer GamerRef
        {
            get { return _gamer; }
        }

        public override bool IsValid
        {
            get { return _playerIndex != -1; }
        }

        public override Texture2D GamerPic
        {
            get
            {
                try
                {
                    return _gamer.GetProfile().GamerPicture;
                }
                catch
                {
                    // it's possible to get a gamer privilege acception here
                    return null;
                }
            }
        }

        public override string GamerTag
        {
            get { return _gamer.Gamertag; }
        }

        public PlayerIndex XNAPlayerIndex
        {
            get{return (PlayerIndex)_playerIndex; }
        }

        public PlayerLocal(int playerIndex)
        {
            _playerIndex = playerIndex;

            // find the gamer
            foreach (SignedInGamer signedInGamer in SignedInGamer.SignedInGamers)
            {
                if (signedInGamer.PlayerIndex == (PlayerIndex)_playerIndex)
                {
                    _gamer = signedInGamer;
                    break;
                }
            }

            // check for errors
            if (_gamer == null)
            {
                Assert.Fatal(false, "Trying to create a local player that isn't a signed in gamer: playerIndex = " + playerIndex);
                _playerIndex = -1;
            }
        }
//#else
//        public override bool  IsValid
//        {
//            get { return _playerIndex != -1; }
//        }

//        public PlayerLocal(int playerIndex)
//        {
//            _playerIndex = playerIndex;
//        }
//#endif

        public bool IsReady;


        public override PlayerCharacterSelector GetCharacterSelector(int playerNum)
        {
            return new PlayerLocalCharacterSelector(playerNum);
        }
    }



    public class PlayerLocalCharacterSelector : PlayerCharacterSelector
    {
        private PlayerLocalCharacterSelectorInputComponent _inputComponent;

        public PlayerLocalCharacterSelector(int playerNum)
            : base(playerNum)
        {
        }

        public override void Tick(MathFreak.GameStates.GameStateCharacterSelectionLobby lobby, float dt)
        {
            if (_inputComponent == null)
            {
                _inputComponent = lobby.GetNextAvailableInputComponent();
                _inputComponent.SetupInputMap((Game.Instance.ActiveGameplaySettings.Players[_playerNum] as PlayerLocal).PlayerIndex);
            }

            // handle radial selection method
            if (_inputComponent.SelectorAngleIsValid)
            {
                lobby.MoveToCharacterAtAngle(_playerNum, _inputComponent.SelectorAngle);
            }

            // handle button presses
            switch (_inputComponent.ActiveButtonPress)
            {
                case PlayerLocalCharacterSelectorInputComponent.BUTTON_NONE:
                    // do nothing
                    break;

                case PlayerLocalCharacterSelectorInputComponent.BUTTON_A:
                    lobby.OnAction_Pressed_A(_playerNum);
                    break;

                case PlayerLocalCharacterSelectorInputComponent.BUTTON_B:
                    lobby.OnAction_Pressed_B(_playerNum);
                    break;

                case PlayerLocalCharacterSelectorInputComponent.BUTTON_PREV:
                    if (!_inputComponent.SelectorAngleIsValid) lobby.MoveToPrevCharacter(_playerNum);
                    break;

                case PlayerLocalCharacterSelectorInputComponent.BUTTON_NEXT:
                    if (!_inputComponent.SelectorAngleIsValid) lobby.MoveToNextCharacter(_playerNum);
                    break;

                default:
                    Assert.Fatal(false, "");
                    break;
            }
        }
    }



    /// <summary>
    /// Handles getting input from a local player for character selection purposes
    /// </summary>
    [TorqueXmlSchemaType]
    public class PlayerLocalCharacterSelectorInputComponent : TorqueComponent, ITickObject
    {
        //======================================================
        #region Static methods, fields, constructors
        #endregion

        //======================================================
        #region Constructors
        #endregion

        //======================================================
        #region Public properties, operators, constants, and enums

        public const int BUTTON_NONE = -1;
        public const int BUTTON_A = 0;
        public const int BUTTON_B = 1;
        public const int BUTTON_NEXT = 2;
        public const int BUTTON_PREV = 3;

        private const float INITIAL_DELAY = 0.5f;   // delay before the first repeat of same movement
        private const float REPEAT_DELAY = 0.12f;    // delay between repeats of the same movement
        private float _delayRemaining = 0.0f;

        private int _activeButtonPress = BUTTON_NONE;
        private int _lastButtonPress = BUTTON_NONE;

        private bool _selectorAngleIsValid = false;
        private float _selectorAngle = 0.0f;

        public T2DSceneObject SceneObject
        {
            get { return Owner as T2DSceneObject; }
        }

        public bool IsAvailable
        {
            get { return _inputMap == null; }
        }

        public int ActiveButtonPress
        {
            get { return _activeButtonPress; }
        }

        public bool SelectorAngleIsValid
        {
            get { return _selectorAngleIsValid; }
        }

        public float SelectorAngle
        {
            get { return _selectorAngle; }
        }

        #endregion

        //======================================================
        #region Public methods

        public virtual void ProcessTick(Move move, float dt)
        {
            _delayRemaining -= dt;

            if (move != null && _inputMap != null)
            {
                // check if any buttons are pressed
                int button = BUTTON_NONE;

                // note: these are mutually exclusive as far as the selection screen is concerned
                if (move.Buttons[BUTTON_B].Pushed)
                {
                    button = BUTTON_B;
                }
                else if (move.Buttons[BUTTON_NEXT].Pushed)
                {
                    button = BUTTON_NEXT;
                }
                else if (move.Buttons[BUTTON_PREV].Pushed)
                {
                    button = BUTTON_PREV;
                }
                else if (move.Buttons[BUTTON_A].Pushed)
                {
                    button = BUTTON_A;
                }

                // decide whether to flag any activity as an actual button press or not depending on repeat-delay stuff and previous button state, etc.
                if (button == BUTTON_NONE)
                {
                    _delayRemaining = 0.0f;
                    _activeButtonPress = BUTTON_NONE;
                    _lastButtonPress = BUTTON_NONE;
                }
                else
                {
                    if (button != _lastButtonPress)
                    {
                        _delayRemaining = INITIAL_DELAY;
                        _activeButtonPress = button;
                        _lastButtonPress = button;
                    }
                    else if (_delayRemaining <= 0.0f)
                    {
                        _delayRemaining = REPEAT_DELAY;
                        _activeButtonPress = button;
                        _lastButtonPress = button;
                    }
                    else
                    {
                        _activeButtonPress = BUTTON_NONE;
                    }
                }

                // record selection angle for left analogue stick if it is valid (must be a minimum amount from the center position to register as being used to select a character)
                float x = move.Sticks[0].X;
                float y = move.Sticks[0].Y;

                // ...must be minimum distance from center
                if (x * x + y * y >= 0.77f)
                {
                    _selectorAngleIsValid = true;
                    _selectorAngle = Util.PointToAngle(x, y);
                }
                // ...else doesn't count
                else
                {
                    _selectorAngleIsValid = false;
                }
            }
        }

        public virtual void InterpolateTick(float k) { }

        public void SetupInputMap(int playerIndex)
        {
            _inputMap = new InputMap();

            int gamepadId = InputManager.Instance.FindDevice("gamepad" + playerIndex);

            if (gamepadId >= 0)
            {
                // buttons and triggers
                _inputMap.BindMove(gamepadId, (int)XGamePadDevice.GamePadObjects.A, MoveMapTypes.Button, BUTTON_A);
                _inputMap.BindMove(gamepadId, (int)XGamePadDevice.GamePadObjects.B, MoveMapTypes.Button, BUTTON_B);
                _inputMap.BindMove(gamepadId, (int)XGamePadDevice.GamePadObjects.LeftShoulder, MoveMapTypes.Button, BUTTON_PREV);
                _inputMap.BindMove(gamepadId, (int)XGamePadDevice.GamePadObjects.RightShoulder, MoveMapTypes.Button, BUTTON_NEXT);
                _inputMap.BindMove(gamepadId, (int)XGamePadDevice.GamePadObjects.LeftTriggerButton, MoveMapTypes.Button, BUTTON_PREV);
                _inputMap.BindMove(gamepadId, (int)XGamePadDevice.GamePadObjects.RightTriggerButton, MoveMapTypes.Button, BUTTON_NEXT);

                // Dpad
                _inputMap.BindMove(gamepadId, (int)XGamePadDevice.GamePadObjects.Left, MoveMapTypes.Button, BUTTON_PREV);
                _inputMap.BindMove(gamepadId, (int)XGamePadDevice.GamePadObjects.Right, MoveMapTypes.Button, BUTTON_NEXT);

                // analogue sticks
                _inputMap.BindMove(gamepadId, (int)XGamePadDevice.GamePadObjects.LeftThumbX, MoveMapTypes.StickAnalogHorizontal, 0);
                _inputMap.BindMove(gamepadId, (int)XGamePadDevice.GamePadObjects.LeftThumbY, MoveMapTypes.StickAnalogVertical, 0);
            }

            int keyboardId = InputManager.Instance.FindDevice("keyboard");

            if (keyboardId >= 0)
            {
                _inputMap.BindMove(keyboardId, (int)Keys.Enter, MoveMapTypes.Button, BUTTON_A);
                _inputMap.BindMove(keyboardId, (int)Keys.Escape, MoveMapTypes.Button, BUTTON_B);
                _inputMap.BindMove(keyboardId, (int)Keys.Left, MoveMapTypes.Button, BUTTON_PREV);
                _inputMap.BindMove(keyboardId, (int)Keys.Right, MoveMapTypes.Button, BUTTON_NEXT);
            }

            _inputMap.MoveManager = new MoveManager();
            ProcessList.Instance.SetMoveManager(Owner, _inputMap.MoveManager);
            InputManager.Instance.PushInputMap(_inputMap);
        }

        #endregion

        //======================================================
        #region Private, protected, internal methods

        protected override bool _OnRegister(TorqueObject owner)
        {
            if (!base._OnRegister(owner) || !(owner is T2DSceneObject))
                return false;

            // tell the process list to notifiy us with ProcessTick and InterpolateTick events
            ProcessList.Instance.AddTickCallback(Owner, this);

            // this isn't an object that should ever get seen on screen.
            SceneObject.Visible = false;

            return true;
        }

        protected override void _OnUnregister()
        {
            if (_inputMap != null)
            {
                ProcessList.Instance.ClearMoveManager(_inputMap.MoveManager);
                InputManager.Instance.PopInputMap(_inputMap);
            }

            base._OnUnregister();
        }

        #endregion

        //======================================================
        #region Private, protected, internal fields

        private InputMap _inputMap;

        #endregion
    }
}
