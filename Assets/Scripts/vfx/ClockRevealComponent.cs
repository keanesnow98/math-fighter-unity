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
    public class ClockRevealComponent : TorqueComponent
    {
        //======================================================
        #region Static methods, fields, constructors
        #endregion

        //======================================================
        #region Constructors
        #endregion

        //======================================================
        #region Public properties, operators, constants, and enums

        [TorqueXmlSchemaType(DefaultValue = "45", IsDefaultValueOf = true)]
        public float SweepAngle
        {
            get { return _sweepAngleInDegrees; }

            set
            {
                _sweepAngleInDegrees = value;

                if (_material != null)
                {
                    _material.SweepAngleInRadiansAngle = MathHelper.ToRadians(_sweepAngleInDegrees);
                }
            }
        }

        #endregion

        //======================================================
        #region Public methods
        #endregion

        //======================================================
        #region Private, protected, internal methods

        protected override bool _OnRegister(TorqueObject owner)
        {
            if (!base._OnRegister(owner) || !(owner is T2DSceneObject))
                return false;

            T2DStaticSprite _sprite = (owner as T2DStaticSprite);
            _material = new ClockRevealMaterial();
            _material.TextureFilename = (_sprite.Material as SimpleMaterial).TextureFilename;
            _sprite.Material = _material;
            _material.SweepAngleInRadiansAngle = MathHelper.ToRadians(_sweepAngleInDegrees);

            return true;
        }

        #endregion

        //======================================================
        #region Private, protected, internal fields

        private ClockRevealMaterial _material;
        private float _sweepAngleInDegrees;

        #endregion
    }
}
