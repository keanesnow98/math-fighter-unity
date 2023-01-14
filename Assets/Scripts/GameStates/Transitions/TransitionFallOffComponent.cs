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
using GarageGames.Torque.Materials;
using System.Diagnostics;

namespace MathFreak.GameStates.Transitions
{
    /// <summary>
    /// Attach this to an object to enable the object to 'fall off' the screen.
    /// E.g. this will be used to transition menu buttons off the screen.
    /// </summary>
    [TorqueXmlSchemaType]
    public class TransitionFallOffComponent : TorqueComponent, ITickObject
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

        public bool HasExited
        {
            get { return _hasExited; }
        }

        public bool IsFallingOff
        {
            get { return _isFallingOff; }
        }

        #endregion

        //======================================================
        #region Public methods

        public virtual void ProcessTick(Move move, float dt)
        {
            // if we aren't in the process of falling off then just return
            if (!_isFallingOff) return;

            // else increment our current velocity
            _velocity += dt * GRAVITY;

            // update our position using our velocity
            SceneObject.Position = new Vector2(_targetPos.X, SceneObject.Position.Y + _velocity);

            // update the alpha level too - (we'll do this as a function of distance from our start position)
            SceneObject.VisibilityLevel = 1.0f - ((SceneObject.Position.Y - _startPosY) / 300.0f);

            // check if reached 'exit' position
            if (SceneObject.Position.Y >= _exitPosY)
            {
                _hasExited = true;
            }

            // check if we've finished falling off
            if (SceneObject.Position.Y >= _targetPos.Y)
            {
                SceneObject.Position = _targetPos;
                SceneObject.VisibilityLevel = 0.0f;

                _isFallingOff = false;
            }
        }

        public virtual void InterpolateTick(float k)
        {
        }

        public void FallOff()
        {
            // set our initial alpha level (completely opaque)
            // NOTE: this also requires we set some properties on the sprite's material so that the alpha will have an effect
            RenderMaterial material = null;

            if (Owner is T2DStaticSprite)
            {
                material = (Owner as T2DStaticSprite).Material;
            }
            else //(Owner is T2DAnimatedSprite) <= we checked this in OnRegister()
            {
                material = (Owner as T2DAnimatedSprite).AnimationData.Material;
            }

            material.IsTranslucent = true;
            (material as SimpleMaterial).IsColorBlended = true;

            SceneObject.VisibilityLevel = 1.0f;

            // get our start position
            _startPosY = SceneObject.Position.Y;

            // set our target and 'exit' positions
            _targetPos = new Vector2(SceneObject.Position.X, _startPosY + FALLOFF_TARGET_DISTANCE);
            _exitPosY = _startPosY + FALLOFF_EXIT_DISTANCE;

            // set the fallingOff flag so that the tick method will do the fall off stuff
            _isFallingOff = true;
            _velocity = 0.0f;
        }

        #endregion

        //======================================================
        #region Private, protected, internal methods

        protected override bool _OnRegister(TorqueObject owner)
        {
            if (!base._OnRegister(owner) || !(owner is T2DSceneObject))
                return false;

            Assert.Fatal(owner is T2DStaticSprite || owner is T2DAnimatedSprite, "HorizontalFlyOnComponent must be attached to a T2DStaticSprite or T2DAnimSprite");

            // tell the process list to notifiy us with ProcessTick and InterpolateTick events
            ProcessList.Instance.AddTickCallback(Owner, this);

            return true;
        }

        #endregion

        //======================================================
        #region Private, protected, internal fields

        private bool _isFallingOff;
        private bool _hasExited;

        private Vector2 _targetPos;

        private float _startPosY;
        private float _exitPosY;

        private float GRAVITY = 100.0f;
        private float _velocity;

        private const float FALLOFF_EXIT_DISTANCE = 100.0f;
        private const float FALLOFF_TARGET_DISTANCE = 720.0f;
 
        #endregion
    }
}
