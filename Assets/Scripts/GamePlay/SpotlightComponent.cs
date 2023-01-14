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
using Microsoft.Xna.Framework.Graphics;
using GarageGames.Torque.Materials;



namespace MathFreak.GamePlay
{
    [TorqueXmlSchemaType]
    public class SpotlightComponent : TorqueComponent, ITickObject
    {
        //======================================================
        #region Static methods, fields, constructors
        #endregion

        //======================================================
        #region Constructors
        #endregion

        //======================================================
        #region Public properties, operators, constants, and enums

        public enum EnumSpotlightState { Idle, RightWrong };

        /// <summary>
        /// Tells the spotlight whether it is the left or right spotlight on the game screen.
        /// This will determine it's range of motion and where it highlights right/wrong when
        /// a player answers a question.
        /// </summary>
        public bool IsLeftSide
        {
            get { return _isLeftSide; }
            set { _isLeftSide = value; }
        }

        #endregion

        //======================================================
        #region Public methods

        public virtual void ProcessTick(Move move, float dt)
        {
            switch (_state)
            {
                case EnumSpotlightState.Idle:
                    ProcessIdle();
                    break;

                case EnumSpotlightState.RightWrong:
                    ProcessRightWrong();
                    break;
            }
        }

        public virtual void InterpolateTick(float k)
        {
        }

        public void DoIdleAnim()
        {
            // if not already doing the idle anim then start doing it
            if (_state != EnumSpotlightState.Idle)
            {
                SetIdleTarget();
                _state = EnumSpotlightState.Idle;
            }
        }

        public void DoRightWrongAnim(Color targetColor)
        {
            SetRightWrongTarget(targetColor);
            _state = EnumSpotlightState.RightWrong;
        }

        #endregion

        //======================================================
        #region Private, protected, internal methods

        protected override bool _OnRegister(TorqueObject owner)
        {
            if (!base._OnRegister(owner) || !(owner is T2DStaticSprite))
                return false;

            ProcessList.Instance.AddTickCallback(Owner, this);

            _sprite = owner as T2DStaticSprite;
            _origPos = _sprite.Position;
            SimpleMaterial material = _sprite.Material as SimpleMaterial;
            material.IsColorBlended = true;
            material.IsTranslucent = true;

            _currentAngle = _sprite.Rotation;

            // setting up the initial [faux] rotation step so idle anim will then start off in the *opposite* direction
            if (_isLeftSide)
            {
                _rotationStep = -0.1f;
            }
            else
            {
                _rotationStep = 0.1f;
            }
                 
            DoIdleAnim();

            return true;
        }

        private void ProcessIdle()
        {
            bool completed = ProcessAnim();

            // if finished the sweep then start a new one
            if (completed)
            {
                SetIdleTarget();
            }
        }

        /// <summary>
        /// Will fill the various 'target' variables with new target values for the idle animation.
        /// (the idle animation sweeps back and forth)
        /// </summary>
        private void SetIdleTarget()
        {
            // choose a target angle and set the rotation step to get there
            if (_rotationStep > 0.0f)
            {
                if (_isLeftSide)
                {
                    _targetAngle = -80.0f;
                }
                else
                {
                    _targetAngle = -25.0f;
                }

                _rotationStep = -0.4f;
            }
            else
            {
                if (_isLeftSide)
                {
                    _targetAngle = 25.0f;
                }
                else
                {
                    _targetAngle = 80.0f;
                }

                _rotationStep = 0.4f;
            }

            // choose target width and the step size to get there
            _targetWidth = 250.0f;

            if (_targetWidth > _sprite.Size.X)
            {
                _widthStep = WIDTHSTEP;
            }
            else
            {
                _widthStep = -WIDTHSTEP;
            }

            // choose target colour and the rgba step increments to get there
            _targetColor = Color.White;
            float r, g, b, a;
            Color currentColor = _sprite.ColorTint;
            const float COLORSTEP = 8.0f;

            if (currentColor.R > _targetColor.R)
            {
                r = -COLORSTEP;
            }
            else
            {
                r = COLORSTEP;
            }

            if (currentColor.G > _targetColor.G)
            {
                g = -COLORSTEP;
            }
            else
            {
                g = COLORSTEP;
            }

            if (currentColor.B > _targetColor.B)
            {
                b = -COLORSTEP;
            }
            else
            {
                b = COLORSTEP;
            }

            if (currentColor.A > _targetColor.A)
            {
                a = -COLORSTEP;
            }
            else
            {
                a = COLORSTEP;
            }

            _colorStep = new Vector4(r, g, b, a);
        }

        private void ProcessRightWrong()
        {
            ProcessAnim();
        }

        private void SetRightWrongTarget(Color targetColor)
        {
            // set target angle and the rotation step to get there
            if (_isLeftSide)
            {
                _targetAngle = -60.0f;
            }
            else
            {
                _targetAngle = 60.0f;
            }

            if (_targetAngle > _currentAngle)
            {
                _rotationStep = 3.0f;
            }
            else
            {
                _rotationStep = -3.0f;
            }

            // choose target width and the step size to get there
            _targetWidth = 1500.0f;

            if (_targetWidth > _sprite.Size.X)
            {
                _widthStep = WIDTHSTEP;
            }
            else
            {
                _widthStep = -WIDTHSTEP;
            }

            // choose target colour and the rgba step increments to get there
            _targetColor = targetColor;
            float r, g, b, a;
            Color currentColor = _sprite.ColorTint;
            const float COLORSTEP = 8.0f;

            if (currentColor.R > _targetColor.R)
            {
                r = -COLORSTEP;
            }
            else
            {
                r = COLORSTEP;
            }

            if (currentColor.G > _targetColor.G)
            {
                g = -COLORSTEP;
            }
            else
            {
                g = COLORSTEP;
            }

            if (currentColor.B > _targetColor.B)
            {
                b = -COLORSTEP;
            }
            else
            {
                b = COLORSTEP;
            }

            if (currentColor.A > _targetColor.A)
            {
                a = -COLORSTEP;
            }
            else
            {
                a = COLORSTEP;
            }

            _colorStep = new Vector4(r, g, b, a);
        }

        /// <summary>
        /// Sets the spotlight at the specified angle.  Does the calculations necessary to rotate the
        /// spotlight around the spotlight beam's base rather than the center of the scene object as
        /// torque would normally do it.
        /// </summary>
        /// <param name="angle">Angle in degrees</param>
        private void SetAngle(float angleInDegrees)
        {
            // set the spot light rotation - this is rotation around the center
            _sprite.Rotation = angleInDegrees;

            // Move the position of the spotlight so it looks like it is rotating around
            // the base of the beam.
            float h = _sprite.Size.Y * 0.5f;    // hypotenuese
            float x = h * (float)System.Math.Sin(MathHelper.ToRadians(angleInDegrees));
            float y = h * (float)System.Math.Cos(MathHelper.ToRadians(angleInDegrees)) - h;
            _sprite.Position = new Vector2(_origPos.X + x, _origPos.Y - y);
        }

        /// <summary>
        /// Animates the spotlight to it's current target parameters.
        /// Returns true when the targets have all been reached.
        /// </summary>
        private bool ProcessAnim()
        {
            // increment towards the targets
            float angle = _currentAngle + _rotationStep;
            float width = _sprite.Size.X + _widthStep;
            int r = (int)_sprite.ColorTint.R + (int)_colorStep.X;
            int g = (int)_sprite.ColorTint.G + (int)_colorStep.Y;
            int b = (int)_sprite.ColorTint.B + (int)_colorStep.Z;
            int a = (int)_sprite.ColorTint.A + (int)_colorStep.W;

            // check if all targets have been reached
            // and also make sure the values don't overshoot the targets
            bool allTargetsReached = true;

            if (IsTargetReached(_targetAngle, angle, _rotationStep))
            {
                angle = _targetAngle;
            }
            else
            {
                allTargetsReached = false;
            }

            if (IsTargetReached(_targetWidth, width, _widthStep))
            {
                width = _targetWidth;
            }
            else
            {
                allTargetsReached = false;
            }

            if (IsTargetReached(_targetColor.R, r, _colorStep.X))
            {
                r = _targetColor.R;
            }
            else
            {
                allTargetsReached = false;
            }

            if (IsTargetReached(_targetColor.G, g, _colorStep.Y))
            {
                g = _targetColor.G;
            }
            else
            {
                allTargetsReached = false;
            }

            if (IsTargetReached(_targetColor.B, b, _colorStep.Z))
            {
                b = _targetColor.B;
            }
            else
            {
                allTargetsReached = false;
            }

            if (IsTargetReached(_targetColor.A, a, _colorStep.W))
            {
                a = _targetColor.A;
            }
            else
            {
                allTargetsReached = false;
            }

            // update the spotlight with the new values
            _sprite.Size = new Vector2(width, _sprite.Size.Y);
            _currentAngle = angle;
            SetAngle(_currentAngle);

            // ...only create a new colour object if we actually need to (if the colour hasn't changed then no need to create a new color object and create unnecessary garbage for collection later)
            Color currentColor = _sprite.ColorTint;

            if (r != currentColor.R || g != currentColor.G || b != currentColor.B || a != currentColor.A)
            {
                _sprite.ColorTint = new Color((byte)r, (byte)g, (byte)b, (byte)a);
            }

            return allTargetsReached;
        }

        private bool IsTargetReached(float target, float current, float step)
        {
            if (step >= 0.0f && current >= target)
            {
                return true;
            }
            else if (step < 0.0f && current <= target)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        #endregion

        //======================================================
        #region Private, protected, internal fields

        private const float WIDTHSTEP = 30.0f;

        private T2DStaticSprite _sprite;

        private bool _isLeftSide;
        private EnumSpotlightState _state;

        private float _targetAngle;
        private float _currentAngle;    // storing our own angle as torque likes to do funky stuff with negative angles, but we want to be able to do some simple increment/decrement math on it
        private float _targetWidth;
        private Color _targetColor;
        private float _rotationStep;
        private Vector4 _colorStep;
        private float _widthStep;
        private Vector2 _origPos;

        #endregion
    }
}
