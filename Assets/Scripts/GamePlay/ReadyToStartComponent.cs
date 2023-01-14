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
using MathFreak.GameStates;



namespace MathFreak.GUIFrameWork
{
    /// <summary>
    /// Polls for input from a specified gamepad and passes the result to the lobby gamestate instance.
    /// </summary>
    [TorqueXmlSchemaType]
    public class ReadyToStartComponent : TorqueComponent, ITickObject
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

        public T2DSceneObject SceneObject
        {
            get { return Owner as T2DSceneObject; }
        }

        #endregion

        //======================================================
        #region Public methods

        public virtual void ProcessTick(Move move, float dt)
        {
            if (move != null && _lobby != null)
            {
                if (move.Buttons[BUTTON_A].Pushed)
                {
                    if (!_wasDownPreviously)
                    {
                        _wasDownPreviously = true;
                        _lobby.ToggleLocalPlayerReady(_player);
                    }
                }
                else
                {
                    _wasDownPreviously = false;
                }
            }
        }

        public virtual void InterpolateTick(float k) { }

        public void SetupInputMap(GameStateSettings lobby, PlayerLocal player)
        {
            _player = player;
            _inputMap = new InputMap();

            int gamepadId = InputManager.Instance.FindDevice("gamepad" + _player.PlayerIndex);

            if (gamepadId >= 0)
            {
                _inputMap.BindMove(gamepadId, (int)XGamePadDevice.GamePadObjects.A, MoveMapTypes.Button, BUTTON_A);
            }

            int keyboardId = InputManager.Instance.FindDevice("keyboard");

            if (keyboardId >= 0)
            {
                _inputMap.BindMove(keyboardId, (int)Keys.Enter, MoveMapTypes.Button, BUTTON_A);
            }

            _inputMap.MoveManager = new MoveManager();
            ProcessList.Instance.SetMoveManager(Owner, _inputMap.MoveManager);
            InputManager.Instance.PushInputMap(_inputMap);

            _lobby = lobby;
        }

        public void ClearInputMap()
        {
            if (_lobby != null)
            {
                _lobby = null;
                _player = null;
                ProcessList.Instance.ClearMoveManager(_inputMap.MoveManager);
                InputManager.Instance.PopInputMap(_inputMap);
            }
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

            _player = null;

            return true;
        }

        protected override void _OnUnregister()
        {
            ClearInputMap();

            base._OnUnregister();
        }

        #endregion

        //======================================================
        #region Private, protected, internal fields

        private InputMap _inputMap;
        private GameStateSettings _lobby;
        private PlayerLocal _player;
        private bool _wasDownPreviously;

        #endregion
    }
}
