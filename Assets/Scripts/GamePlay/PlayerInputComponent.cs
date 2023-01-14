using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

using GarageGames.Torque.Core;
using GarageGames.Torque.Util;
using GarageGames.Torque.Sim;
using GarageGames.Torque.T2D;
using GarageGames.Torque.SceneGraph;
using GarageGames.Torque.MathUtil;
using GarageGames.Torque.Platform;
using MathFreak.GamePlay;



namespace MathFreak.GUIFrameWork
{
    /// <summary>
    /// Polls for input from a specified gamepad and passes the result to a specified PlayerLocal object.
    /// </summary>
    [TorqueXmlSchemaType]
    public class PlayerInputComponent : TorqueComponent, ITickObject
    {
        //======================================================
        #region Static methods, fields, constructors
        #endregion

        //======================================================
        #region Constructors
        #endregion

        //======================================================
        #region Public properties, operators, constants, and enums

        public const int BUTTON_A = 0;
        public const int BUTTON_B = 1;
        public const int BUTTON_X = 2;
        public const int BUTTON_Y = 3;
        public const int HINT = 4;
        public const int SUPERATTACK = 5;
        public const int TAUNT = 6;

        // DEBUG/CHEAT/TESTING
        public const int CHEAT = 7;
        public const int FREEZE = 8;

        public T2DSceneObject SceneObject
        {
            get { return Owner as T2DSceneObject; }
        }

        #endregion

        //======================================================
        #region Public methods

        public virtual void ProcessTick(Move move, float dt)
        {
            if (move != null && _player != null)
            {
                // note: these are mutually exclusive as far as the game is concerned
                if (move.Buttons[BUTTON_A].Pushed)
                {
                    _player.OnButtonPressed(Player.EnumGamepadButton.A);
                }
                else if (move.Buttons[BUTTON_B].Pushed)
                {
                    _player.OnButtonPressed(Player.EnumGamepadButton.B);
                }
                else if (move.Buttons[BUTTON_X].Pushed)
                {
                    _player.OnButtonPressed(Player.EnumGamepadButton.X);
                }
                else if (move.Buttons[BUTTON_Y].Pushed)
                {
                    _player.OnButtonPressed(Player.EnumGamepadButton.Y);
                }

                // note: these can be pressed simultaneously with other buttons
                _player.HintPressed = move.Buttons[HINT].Pushed;
                _player.SuperAttackPressed = move.Buttons[SUPERATTACK].Pushed;
                _player.TauntPressed = move.Buttons[TAUNT].Pushed;
                //_player.CheatPressed = move.Buttons[CHEAT].Pushed;
                _player.FreezePressed = move.Buttons[FREEZE].Pushed;
            }
        }

        public virtual void InterpolateTick(float k) { }

        public void SetupInputMap(PlayerLocal player)
        {
            _inputMap = new InputMap();

            int gamepadId = InputManager.Instance.FindDevice("gamepad" + player.PlayerIndex);

            if (gamepadId >= 0)
            {
                _inputMap.BindMove(gamepadId, (int)XGamePadDevice.GamePadObjects.A, MoveMapTypes.Button, BUTTON_A);
                _inputMap.BindMove(gamepadId, (int)XGamePadDevice.GamePadObjects.B, MoveMapTypes.Button, BUTTON_B);
                _inputMap.BindMove(gamepadId, (int)XGamePadDevice.GamePadObjects.X, MoveMapTypes.Button, BUTTON_X);
                _inputMap.BindMove(gamepadId, (int)XGamePadDevice.GamePadObjects.Y, MoveMapTypes.Button, BUTTON_Y);
                _inputMap.BindMove(gamepadId, (int)XGamePadDevice.GamePadObjects.LeftShoulder, MoveMapTypes.Button, TAUNT);
                _inputMap.BindMove(gamepadId, (int)XGamePadDevice.GamePadObjects.RightShoulder, MoveMapTypes.Button, SUPERATTACK);
                //_inputMap.BindMove(gamepadId, (int)XGamePadDevice.GamePadObjects.LeftTriggerButton, MoveMapTypes.Button, CHEAT);
                _inputMap.BindMove(gamepadId, (int)XGamePadDevice.GamePadObjects.RightTriggerButton, MoveMapTypes.Button, HINT);
                _inputMap.BindMove(gamepadId, (int)XGamePadDevice.GamePadObjects.RightThumbButton, MoveMapTypes.Button, FREEZE);
                _inputMap.BindMove(gamepadId, (int)XGamePadDevice.GamePadObjects.Back, MoveMapTypes.Button, FREEZE);
            }

            int keyboardId = InputManager.Instance.FindDevice("keyboard");

            if (keyboardId >= 0)
            {
                _inputMap.BindMove(keyboardId, (int)Keys.End, MoveMapTypes.Button, BUTTON_A);
                _inputMap.BindMove(keyboardId, (int)Keys.PageDown, MoveMapTypes.Button, BUTTON_B);
                _inputMap.BindMove(keyboardId, (int)Keys.Home, MoveMapTypes.Button, BUTTON_X);
                _inputMap.BindMove(keyboardId, (int)Keys.PageUp, MoveMapTypes.Button, BUTTON_Y);
                _inputMap.BindMove(keyboardId, (int)Keys.Divide, MoveMapTypes.Button, TAUNT);
                _inputMap.BindMove(keyboardId, (int)Keys.Subtract, MoveMapTypes.Button, SUPERATTACK);
                //_inputMap.BindMove(keyboardId, (int)Keys.Multiply, MoveMapTypes.Button, CHEAT);
                _inputMap.BindMove(keyboardId, (int)Keys.Add, MoveMapTypes.Button, HINT);
                _inputMap.BindMove(keyboardId, (int)Keys.Enter, MoveMapTypes.Button, FREEZE);
            }

            _inputMap.MoveManager = new MoveManager();
            ProcessList.Instance.SetMoveManager(Owner, _inputMap.MoveManager);
            InputManager.Instance.PushInputMap(_inputMap);

            // and finally we will also reference the player themselves so we can tell them when buttons get pressed
            _player = player;
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
            if (_player != null)
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
        private PlayerLocal _player;

        #endregion
    }
}
