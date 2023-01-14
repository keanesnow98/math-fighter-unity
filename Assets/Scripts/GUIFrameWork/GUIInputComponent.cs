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
using Microsoft.Xna.Framework.GamerServices;



namespace MathFreak.GUIFrameWork
{
    /// <summary>
    /// This component does the actual polling of the input device state and then passes the information
    /// to the GUI manager for processing.  This way we can swap between GUIInputComponents that are
    /// designed for mapping the input differently and the GUIManager doesn't need to worry about this
    /// lower level stuff.
    /// </summary>
    [TorqueXmlSchemaType]
    public class GUIInputComponent : TorqueComponent, ITickObject
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
        public const int BUTTON_BACK = 4;
        public const int BUTTON_START = 5;
        public const int BUTTON_LSHOULDER = 6;
        public const int BUTTON_RSHOULDER = 7;
        public const int BUTTON_LTRIGGER = 8;
        public const int BUTTON_RTRIGGER = 9;

        public T2DSceneObject SceneObject
        {
            get { return Owner as T2DSceneObject; }
        }

        #endregion

        //======================================================
        #region Public methods

        public virtual void ProcessTick(Move move, float dt)
        {
            if (move != null && _inputEnabled && !Guide.IsVisible)
            {
                //Debug.WriteLine("move: " + move.Sticks[0].Y);
                GUIManager.Instance.ProcessInput(move, dt);
            }
        }

        public virtual void InterpolateTick(float k) { }

        public void DisableInput()
        {
            _inputEnabled = false;
        }

        public void EnableInput()
        {
            _inputEnabled = true;
        }

        #endregion

        //======================================================
        #region Private, protected, internal methods

        protected override bool _OnRegister(TorqueObject owner)
        {
            if (!base._OnRegister(owner) || !(owner is T2DSceneObject))
                return false;

            _SetupInputMap();
            _inputEnabled = false;  // not enabled until we're told to be!
            GUIManager.Instance.SetInputSource(this);

            // tell the process list to notifiy us with ProcessTick and InterpolateTick events
            ProcessList.Instance.AddTickCallback(Owner, this);

            SceneObject.Visible = false;

            return true;
        }

        protected override void _OnUnregister()
        {
            ProcessList.Instance.ClearMoveManager(_inputMap.MoveManager);
            InputManager.Instance.PopInputMap(_inputMap);

            base._OnUnregister();
        }

        private void _SetupInputMap()
        {
            _inputMap = new InputMap();

            int gamepadId = InputManager.Instance.FindDevice("gamepad" + Game.Instance.GamepadPlayerIndex);

            if (gamepadId >= 0)
            {
                // left analogue stick
                _inputMap.BindMove(gamepadId, (int)XGamePadDevice.GamePadObjects.LeftThumbLeftButton, MoveMapTypes.StickDigitalLeft, 0);
                _inputMap.BindMove(gamepadId, (int)XGamePadDevice.GamePadObjects.LeftThumbRightButton, MoveMapTypes.StickDigitalRight, 0);
                _inputMap.BindMove(gamepadId, (int)XGamePadDevice.GamePadObjects.LeftThumbUpButton, MoveMapTypes.StickDigitalUp, 0);
                _inputMap.BindMove(gamepadId, (int)XGamePadDevice.GamePadObjects.LeftThumbDownButton, MoveMapTypes.StickDigitalDown, 0);

                // Dpad
                _inputMap.BindMove(gamepadId, (int)XGamePadDevice.GamePadObjects.Left, MoveMapTypes.StickDigitalLeft, 0);
                _inputMap.BindMove(gamepadId, (int)XGamePadDevice.GamePadObjects.Right, MoveMapTypes.StickDigitalRight, 0);
                _inputMap.BindMove(gamepadId, (int)XGamePadDevice.GamePadObjects.Up, MoveMapTypes.StickDigitalUp, 0);
                _inputMap.BindMove(gamepadId, (int)XGamePadDevice.GamePadObjects.Down, MoveMapTypes.StickDigitalDown, 0);

                // buttons
                _inputMap.BindMove(gamepadId, (int)XGamePadDevice.GamePadObjects.A, MoveMapTypes.Button, BUTTON_A);
                _inputMap.BindMove(gamepadId, (int)XGamePadDevice.GamePadObjects.B, MoveMapTypes.Button, BUTTON_B);
                _inputMap.BindMove(gamepadId, (int)XGamePadDevice.GamePadObjects.X, MoveMapTypes.Button, BUTTON_X);
                _inputMap.BindMove(gamepadId, (int)XGamePadDevice.GamePadObjects.Y, MoveMapTypes.Button, BUTTON_Y);
                _inputMap.BindMove(gamepadId, (int)XGamePadDevice.GamePadObjects.Back, MoveMapTypes.Button, BUTTON_BACK);
                _inputMap.BindMove(gamepadId, (int)XGamePadDevice.GamePadObjects.Start, MoveMapTypes.Button, BUTTON_START);
                _inputMap.BindMove(gamepadId, (int)XGamePadDevice.GamePadObjects.LeftShoulder, MoveMapTypes.Button, BUTTON_LSHOULDER);
                _inputMap.BindMove(gamepadId, (int)XGamePadDevice.GamePadObjects.RightShoulder, MoveMapTypes.Button, BUTTON_RSHOULDER);
                _inputMap.BindMove(gamepadId, (int)XGamePadDevice.GamePadObjects.LeftTriggerButton, MoveMapTypes.Button, BUTTON_LTRIGGER);
                _inputMap.BindMove(gamepadId, (int)XGamePadDevice.GamePadObjects.RightTriggerButton, MoveMapTypes.Button, BUTTON_RTRIGGER);
            }

            int keyboardId = InputManager.Instance.FindDevice("keyboard");

            if (keyboardId >= 0)
            {
                _inputMap.BindMove(keyboardId, (int)Keys.Right, MoveMapTypes.StickDigitalRight, 0);
                _inputMap.BindMove(keyboardId, (int)Keys.Left, MoveMapTypes.StickDigitalLeft, 0);
                _inputMap.BindMove(keyboardId, (int)Keys.Up, MoveMapTypes.StickDigitalUp, 0);
                _inputMap.BindMove(keyboardId, (int)Keys.Down, MoveMapTypes.StickDigitalDown, 0);

                _inputMap.BindMove(keyboardId, (int)Keys.Enter, MoveMapTypes.Button, BUTTON_A);
                _inputMap.BindMove(keyboardId, (int)Keys.Space, MoveMapTypes.Button, BUTTON_A);
                _inputMap.BindMove(keyboardId, (int)Keys.Escape, MoveMapTypes.Button, BUTTON_B);
                _inputMap.BindMove(keyboardId, (int)Keys.End, MoveMapTypes.Button, BUTTON_A);
                _inputMap.BindMove(keyboardId, (int)Keys.PageDown, MoveMapTypes.Button, BUTTON_B);
                _inputMap.BindMove(keyboardId, (int)Keys.Home, MoveMapTypes.Button, BUTTON_X);
                _inputMap.BindMove(keyboardId, (int)Keys.PageUp, MoveMapTypes.Button, BUTTON_Y);
                _inputMap.BindMove(keyboardId, (int)Keys.Divide, MoveMapTypes.Button, BUTTON_LSHOULDER);
                _inputMap.BindMove(keyboardId, (int)Keys.Subtract, MoveMapTypes.Button, BUTTON_RSHOULDER);
                _inputMap.BindMove(keyboardId, (int)Keys.Multiply, MoveMapTypes.Button, BUTTON_LTRIGGER);
                _inputMap.BindMove(keyboardId, (int)Keys.Add, MoveMapTypes.Button, BUTTON_RTRIGGER);
            }

            _inputMap.MoveManager = new MoveManager();
            _inputMap.MoveManager.ConfigureStickVerticalTracking(0, 0.001f, new float[] { 0.0f, 1.0f }, 0.001f, new float[] { 1.0f, 0.0f });
            _inputMap.MoveManager.ConfigureStickHorizontalTracking(0, 0.001f, new float[] { 0.0f, 1.0f }, 0.001f, new float[] { 1.0f, 0.0f });
            ProcessList.Instance.SetMoveManager(Owner, _inputMap.MoveManager);
            InputManager.Instance.PushInputMap(_inputMap);
        }

        #endregion

        //======================================================
        #region Private, protected, internal fields

        private InputMap _inputMap;
        private bool _inputEnabled;

        #endregion
    }
}
