using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework;

using GarageGames.Torque.Core;
using GarageGames.Torque.Util;
using GarageGames.Torque.Sim;
using GarageGames.Torque.T2D;
using GarageGames.Torque.SceneGraph;
using GarageGames.Torque.MathUtil;
using MathFreak.Text;

namespace MathFreak
{
    /// <summary>
    /// Provides a display with a message followed by '.....' that is 'animated' to show
    /// something is happening and the game has not stalled.
    /// </summary>
    [TorqueXmlSchemaType]
    public class LoadSearchComponent : MathMultiTextComponent, ITickObject
    {
        //======================================================
        #region Static methods, fields, constructors
        #endregion

        //======================================================
        #region Constructors
        #endregion

        //======================================================
        #region Public properties, operators, constants, and enums
        #endregion

        //======================================================
        #region Public methods

        public virtual void ProcessTick(Move move, float dt)
        {
            _elapsedTime += dt;

            if (_elapsedTime > 0.2f)
            {
                _currentDotCount++;

                if (_currentDotCount > 10)
                {
                    _currentDotCount = 1;
                }

                TextValue = _message + new string('.', _currentDotCount);

                _elapsedTime -= 0.2f;
            }
        }

        public virtual void InterpolateTick(float k)
        {
        }

        public void SetMessage(string message)
        {
            _message = message;
        }

        #endregion

        //======================================================
        #region Private, protected, internal methods

        protected override bool _OnRegister(TorqueObject owner)
        {
            if (!base._OnRegister(owner) || !(owner is T2DSceneObject))
                return false;

            ProcessList.Instance.AddTickCallback(Owner, this);
            SetMessage(TextValue);

            return true;
        }

        #endregion

        //======================================================
        #region Private, protected, internal fields

        private string _message;
        private float _elapsedTime;
        private int _currentDotCount;

        #endregion
    }
}
