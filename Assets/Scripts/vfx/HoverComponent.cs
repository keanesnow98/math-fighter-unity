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
using System.Diagnostics;



namespace MathFreak.vfx
{
    [TorqueXmlSchemaType]
    public class HoverComponent : TorqueComponent, ITickObject
    {
        //======================================================
        #region Static methods, fields, constructors
        #endregion

        //======================================================
        #region Constructors
        #endregion

        //======================================================
        #region Public properties, operators, constants, and enums

        public T2DSceneObject SceneObject
        {
            get { return Owner as T2DSceneObject; }
        }

        // This is how far through the hover movement the hovering should start (range: 0 - 1 inclusive)
        public float HoverPhasePosition
        {
            get { return _hoverTimeElapsed / _hoverDuration; }
            set { _hoverTimeElapsed = value * _hoverDuration; }
        }

        [TorqueXmlSchemaType(DefaultValue = "2", IsDefaultValueOf = true)]
        public float HoverDuration
        {
            get { return _hoverDuration; }
            set { _hoverDuration = value; }
        }

        [TorqueXmlSchemaType(DefaultValue = "10", IsDefaultValueOf = true)]
        public float HoverRange
        {
            get { return _hoverRange; }
            set { _hoverRange = value; }
        }

        #endregion

        //======================================================
        #region Public methods

        public virtual void ProcessTick(Move move, float dt)
        {
            _hoverTimeElapsed += dt;

            SceneObject.Position = new Vector2(SceneObject.Position.X, InterpolationHelper.Interpolate(_hoverStartY, _hoverTargetY, _hoverTimeElapsed / _hoverInterpolationDuration, InterpolationMode.EaseInOut));

            // check if we've finished moving in the current direction
            float dy = _hoverTargetY - SceneObject.Position.Y;

            if (_hoverTimeElapsed >= _hoverDurationEnd)
            {
                // chose the new target position (opposite direction to the way we were just going)
                if (_hoverTargetY > _hoverStartY)
                {
                    _hoverTargetY = _hoverAnchorY - _hoverRange;
                }
                else
                {
                    _hoverTargetY = _hoverAnchorY + _hoverRange;
                }

                // start position is wherever we are at the moment
                _hoverStartY = SceneObject.Position.Y;

                // reset the clock...
                _hoverTimeElapsed = _hoverDurationStart;
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

//            _rnd = new Random();

            _hoverAnchorY = SceneObject.Position.Y;
            _hoverStartY = _hoverAnchorY - _hoverRange;
            _hoverTargetY = _hoverAnchorY + _hoverRange;

            // calculate the duration time to use when interpolating (we are only going to use the middle part of the easeinout curve)
            _hoverInterpolationDuration = _hoverDuration * 5.0f;

            _hoverDurationStart = (_hoverInterpolationDuration - _hoverDuration) * 0.5f;
            _hoverDurationEnd = (_hoverInterpolationDuration + _hoverDuration) * 0.5f;

            _hoverTimeElapsed = _hoverDurationStart;

            // tell the process list to notifiy us with ProcessTick and InterpolateTick events
            ProcessList.Instance.AddTickCallback(Owner, this);

            return true;
        }

        #endregion

        //======================================================
        #region Private, protected, internal fields

        private float _hoverDuration;
        private float _hoverInterpolationDuration;
        private float _hoverDurationStart;
        private float _hoverDurationEnd;
        private float _hoverRange;
        private float _hoverAnchorY;
        private float _hoverStartY;
        private float _hoverTargetY;
        private float _hoverTimeElapsed;
//        private Random _rnd;

        #endregion
    }
}
