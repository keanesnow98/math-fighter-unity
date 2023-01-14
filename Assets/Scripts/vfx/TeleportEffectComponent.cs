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
using System.Xml.Serialization;



namespace MathFreak.vfx
{
    [TorqueXmlSchemaType]
    public class TeleportEffectComponent : TorqueComponent
    {
        //======================================================
        #region Static methods, fields, constructors
        #endregion

        //======================================================
        #region Constructors
        #endregion

        //======================================================
        #region Public properties, operators, constants, and enums

        [XmlIgnore]
        public float AnimTime
        {
            get { return _animTime; }

            set
            {
                _animTime = value;

                if (_material != null)
                {
                    _material.AnimTime = _animTime;
                }
            }
        }

        [XmlIgnore]
        public Vector2 TeleportOffset
        {
            get { return _teleportOffset; }

            set
            {
                _teleportOffset = value;

                if (_material != null)
                {
                    _material.TeleportOffset = _teleportOffset;
                }
            }
        }

        [XmlIgnore]
        public SimpleMaterial BaseTexture
        {
            set { _material.SetTexture(value.Texture.Instance); }
        }

        public RenderMaterial TeleportTexture
        {
            get { return _teleportTexture; }

            set
            {
                _teleportTexture = value;

                if (_material != null)
                {
                    _material.TeleportTexture = (_teleportTexture as SimpleMaterial).Texture.Instance;
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
            _material = new TeleportMaterial();
            _material.TextureFilename = (_sprite.Material as SimpleMaterial).TextureFilename;
            _sprite.Material = _material;
            _material.TeleportTexture = (_teleportTexture as SimpleMaterial).Texture.Instance;

            return true;
        }

        #endregion

        //======================================================
        #region Private, protected, internal fields

        private TeleportMaterial _material;
        private float _animTime;
        private Vector2 _teleportOffset;
        private RenderMaterial _teleportTexture;

        #endregion
    }
}
