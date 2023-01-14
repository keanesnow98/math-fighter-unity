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
    /// Attach this to an object to enable the object to 'fly on' to the screen.
    /// E.g. this will be used to transition menu buttons onto the screen.
    /// 
    /// NOTE: for simplicity and speed of coding at the moment we just assume always flying
    /// on from the right of the screen.  Can update this to bi-directional later if need be.
    /// </summary>
    [TorqueXmlSchemaType]
    public class TransitionHorizontalFlyOnComponent : TorqueComponent, ITickObject
    {
        //======================================================
        #region Static methods, fields, constructors
        #endregion

        //======================================================
        #region Constructors
        #endregion

        //======================================================
        #region Public properties, operators, constants, and enums

        [TorqueXmlSchemaType(DefaultValue = "300.000")]
        public float ActivationPosX
        {
            get { return _activationPosX; }
            set { _activationPosX = value; }
        }

        public T2DSceneObject SceneObject
        {
            get { return Owner as T2DSceneObject; }
        }

        public bool IsActivated
        {
            get { return _isActivated; }
        }

        public bool IsFlyingOn
        {
            get { return _isFlyingOn; }
        }

        #endregion

        //======================================================
        #region Public methods

        public virtual void ProcessTick(Move move, float dt)
        {
            // if we aren't in the process of flyingOn then just return
            if (!_isFlyingOn) return;

            // else interpolate our position and move across the screen
            _flyonElapsed += dt;

            SceneObject.Position = new Vector2(InterpolationHelper.Interpolate(_startPosX, _targetPos.X - _overshootOffsetX, _flyonElapsed / FLYON_DURATION, InterpolationMode.FlyOn), _targetPos.Y);

            // update the alpha level too
            SceneObject.VisibilityLevel = InterpolationHelper.Interpolate(0.0f, 1.0f, _flyonElapsed / FLYON_DURATION, InterpolationMode.FlyOn);

            // check if reached activation position
            if (SceneObject.Position.X <= _activationPosX)
            {
                _isActivated = true;
            }

            // check if we've finished flying on
            if (SceneObject.Position.X <= _targetPos.X + 2.5f)
            {
                //Debug.WriteLine("finished flying on (" + Owner.Name + ") - elapsed time: " + _flyonElapsed);

                SceneObject.Position = _targetPos;
                SceneObject.VisibilityLevel = 1.0f;

                _isFlyingOn = false;
            }
        }

        public virtual void InterpolateTick(float k)
        {
        }

        public void FlyOn()
        {
            // make ourselves visible (incase we were hidden by the owning gamestate)
            SceneObject.Visible = true;

            // set our initial alpha level (completely transparent)
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

            SceneObject.VisibilityLevel = 0.0f;

            // set our initial position (offscreen) and size
            SceneObject.Position = new Vector2(_startPosX, _targetPos.Y);

            // set the flyingOn flag so that the tick method will do the fly on stuff
            _isFlyingOn = true;
            _flyonElapsed = 0.0f;
        }

        public void CancelFlyon()
        {
            _isFlyingOn = false;
        }

        #endregion

        //======================================================
        #region Private, protected, internal methods

        protected override bool _OnRegister(TorqueObject owner)
        {
            if (!base._OnRegister(owner) || !(owner is T2DSceneObject))
                return false;

            Assert.Fatal(owner is T2DStaticSprite || owner is T2DAnimatedSprite, "HorizontalFlyOnComponent must be attached to a T2DStaticSprite or T2DAnimSprite");

            // record our initial position and size (we'll use this later to make sure we get positioned correctly when we fly on)
            _targetPos = SceneObject.Position;

            // tell the process list to notifiy us with ProcessTick and InterpolateTick events
            ProcessList.Instance.AddTickCallback(Owner, this);

            return true;
        }

        #endregion

        //======================================================
        #region Private, protected, internal fields

        private bool _isFlyingOn;
        private bool _isActivated;

        private Vector2 _targetPos;

        private const float _startPosX = 640.0f;
        private float _activationPosX = 300.0f;
        private const float _overshootOffsetX = 10.0f;

        private const float FLYON_DURATION = 2.0f;
        private float _flyonElapsed;

        #endregion
    }
}
