using System;
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


namespace MathFreak
{
    /// <summary>
    /// This component is used to detect which gamepad a player is using to play the game.
    /// The component will create an input map tied to a specified player index.
    /// If the component recieves input from the player's gamepad then the Game instance is notified.
    /// 
    /// This component should be attached to multiple offscreen objects in a SplashScreen that
    /// prompts the player to press 'start'.  One object should be used per player index (usually up to 4).
    /// </summary>
    [TorqueXmlSchemaType]
    public class ActiveGamepadDetector : TorqueComponent, ITickObject
    {
        //======================================================
        #region Static methods, fields, constructors
        #endregion

        //======================================================
        #region Constructors
        #endregion

        //======================================================
        #region Public properties, operators, constants, and enums

        public int PlayerIndex
        {
            get { return _playerIndex; }
            set { _playerIndex = value; }
        }

        public bool Enabled
        {
            get { return _enabled; }
            set { _enabled = value; }
        }

        public T2DSceneObject SceneObject
        {
            get { return Owner as T2DSceneObject; }
        }

        #endregion

        //======================================================
        #region Public methods

        public virtual void ProcessTick(Move move, float dt)
        {
            if (!_enabled) return;

            if (move != null)
            {
                if (move.Buttons[0].Pushed)
                {
                    Game.Instance.OnActiveGamepadDetected(_playerIndex);
                }
            }
        }

        public virtual void InterpolateTick(float k)
        {
        }

        #endregion

        //======================================================
        #region Private, protected, internal methods

        protected override bool _OnRegister(TorqueObject owner)
        {
            if (!base._OnRegister(owner) || !(owner is T2DSceneObject))
                return false;

            Assert.Fatal(_playerIndex >= 0 && _playerIndex < MAX_PLAYERS, "Player index must be within the ranget 0-" + (MAX_PLAYERS - 1) + " (inclusive)");

            _SetupInputMap();

            // tell the process list to notifiy us with ProcessTick and InterpolateTick events
            ProcessList.Instance.AddTickCallback(Owner, this);

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

        private void _SetupInputMap()
        {
            int gamepadId = InputManager.Instance.FindDevice("gamepad" + _playerIndex);

            if (gamepadId >= 0)
            {
                _inputMap = new InputMap();

                _inputMap.BindMove(gamepadId, (int)XGamePadDevice.GamePadObjects.Start, MoveMapTypes.Button, 0);

                _inputMap.MoveManager = new MoveManager();
                ProcessList.Instance.SetMoveManager(Owner, _inputMap.MoveManager);
                InputManager.Instance.PushInputMap(_inputMap);
            }
            // NOTE: we can have an arbitrary number of gamepad detectors, but the system we're running on might have a limit to the number of devices that can be connected.
            // So delete ourselves if we didn't find a gamepad id.
            else
            {
                SceneObject.MarkForDelete = true;
            }
        }

        #endregion

        //======================================================
        #region Private, protected, internal fields

        private const int MAX_PLAYERS = 4;

        private InputMap _inputMap;
        private int _playerIndex;
        private bool _enabled;

        #endregion
    }
}
