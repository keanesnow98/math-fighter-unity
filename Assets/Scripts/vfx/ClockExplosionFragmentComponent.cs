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
using GarageGames.Torque.Materials;



namespace MathFreak.vfx
{
    [TorqueXmlSchemaType]
    public class ClockExplosionFragmentComponent : TorqueComponent, ITickObject
    {
        private const float FADEOUT_DURATION = 1.5f;    // in seconds

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
            if (!_isExploding) return;

            if (_sprite.VisibilityLevel <= 0.0f)
            {
                _isExploding = false;
                _sprite.MarkForDelete = true;
            }

            _elapsedTime += dt;

            _sprite.Physics.VelocityY += dt * 500.0f;
            _sprite.VisibilityLevel = MathHelper.SmoothStep(1.0f, -1.0f, ((_elapsedTime / FADEOUT_DURATION) * 0.5f));
        }

        public virtual void InterpolateTick(float k)
        {
            // todo: interpolate between ticks as needed here
        }

        public void StartFX(float xOffset, float yOffset)
        {
            _isExploding = true;

            _sprite.Physics.AngularVelocity = xOffset * 50.0f;
            _sprite.Physics.VelocityX = xOffset * (50.0f + (float)Game.Instance.Rnd.NextDouble() * 50.0f);
            _sprite.Physics.VelocityY = yOffset * (50.0f + (float)Game.Instance.Rnd.NextDouble() * 50.0f);

            SimpleMaterial material = _sprite.Material as SimpleMaterial;
            material.IsColorBlended = true;
            material.IsTranslucent = true;
        }

        #endregion

        //======================================================
        #region Private, protected, internal methods

        protected override bool _OnRegister(TorqueObject owner)
        {
            if (!base._OnRegister(owner) || !(owner is T2DStaticSprite))
                return false;

            _sprite = owner as T2DStaticSprite;

            ProcessList.Instance.AddTickCallback(Owner, this);

            return true;
        }

        #endregion

        //======================================================
        #region Private, protected, internal fields

        private T2DStaticSprite _sprite;
        private bool _isExploding;
        private float _elapsedTime;

        #endregion
    }
}
