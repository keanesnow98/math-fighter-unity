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
using Microsoft.Xna.Framework.Graphics;



namespace MathFreak.vfx
{
    [TorqueXmlSchemaType]
    public class RadialGradiantComponent : TorqueComponent
    {
        //======================================================
        #region Static methods, fields, constructors
        #endregion

        //======================================================
        #region Constructors
        #endregion

        //======================================================
        #region Public properties, operators, constants, and enums

        [TorqueXmlSchemaType(DefaultValue = "0 0 0 0", IsDefaultValueOf = true)]
        public Vector4 InnerColor
        {
            get { return _innerColor; }

            set
            {
                _innerColor = value;

                if (_material != null)
                {
                    _material.InnerColor = _innerColor;
                }
            }
        }

        [TorqueXmlSchemaType(DefaultValue = "1 1 1 1", IsDefaultValueOf = true)]
        public Vector4 OuterColor
        {
            get { return _outerColor; }

            set
            {
                _outerColor = value;

                if (_material != null)
                {
                    _material.OuterColor = _outerColor;
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
            _material = new RadialGradiantMaterial();
            _material.TextureFilename = (_sprite.Material as SimpleMaterial).TextureFilename;
            _sprite.Material = _material;
            _material.InnerColor = _innerColor;
            _material.OuterColor = _outerColor;

            return true;
        }

        #endregion

        //======================================================
        #region Private, protected, internal fields

        private RadialGradiantMaterial _material;
        private Vector4 _innerColor;
        private Vector4 _outerColor;

        #endregion
    }
}
