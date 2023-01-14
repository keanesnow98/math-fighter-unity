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
    public class OffsetTextureComponent : TorqueComponent
    {
        //======================================================
        #region Static methods, fields, constructors
        #endregion

        //======================================================
        #region Constructors
        #endregion

        //======================================================
        #region Public properties, operators, constants, and enums

        [TorqueXmlSchemaType(DefaultValue = "0 0", IsDefaultValueOf = true)]
        public Vector2 Offset
        {
            get { return _offset; }

            set
            {
                _offset = value;

                if (_material != null)
                {
                    _material.Offset = _offset;
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
            _material = new OffsetTextureMaterial();
            _material.TextureFilename = (_sprite.Material as SimpleMaterial).TextureFilename;
            _sprite.Material = _material;
            _material.Offset = _offset;

            return true;
        }

        #endregion

        //======================================================
        #region Private, protected, internal fields

        private OffsetTextureMaterial _material;
        private Vector2 _offset;

        #endregion
    }
}
