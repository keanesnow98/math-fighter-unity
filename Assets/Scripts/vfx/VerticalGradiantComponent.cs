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
    public class VerticalGradiantComponent : TorqueComponent
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
        public Vector4 BottomColor
        {
            get { return _bottomColor; }

            set
            {
                _bottomColor = value;

                if (_material != null)
                {
                    _material.BottomColor = _bottomColor;
                }
            }
        }

        [TorqueXmlSchemaType(DefaultValue = "1 1 1 1", IsDefaultValueOf = true)]
        public Vector4 TopColor
        {
            get { return _topColor; }

            set
            {
                _topColor = value;

                if (_material != null)
                {
                    _material.TopColor = _topColor;
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
            _material = new VerticalGradiantMaterial();
            _material.TextureFilename = (_sprite.Material as SimpleMaterial).TextureFilename;
            _sprite.Material = _material;
            _material.BottomColor = _bottomColor;
            _material.TopColor = _topColor;

            return true;
        }

        #endregion

        //======================================================
        #region Private, protected, internal fields

        private VerticalGradiantMaterial _material;
        private Vector4 _bottomColor;
        private Vector4 _topColor;

        #endregion
    }
}
