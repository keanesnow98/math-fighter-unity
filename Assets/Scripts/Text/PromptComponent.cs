using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

using GarageGames.Torque.Core;
using GarageGames.Torque.Util;
using GarageGames.Torque.Sim;
using GarageGames.Torque.T2D;
using GarageGames.Torque.SceneGraph;
using GarageGames.Torque.MathUtil;
using GarageGames.Torque.GUI;
using GarageGames.Torque.Materials;
using GarageGames.Torque.XNA;
using GarageGames.Torque.GFX;



namespace MathFreak.Text
{
    /// <summary>
    /// This component allows you set T2DStaticSprites to have an icon and text displayed on them.
    /// i.e. a 'prompt'
    /// </summary>
    [TorqueXmlSchemaType]
    public class PromptComponent : TorqueComponent
    {
        //======================================================
        #region Static methods, fields, constructors
        #endregion

        //======================================================
        #region Constructors
        #endregion

        //======================================================
        #region Public properties, operators, constants, and enums

        public String TextValue
        {
            get { return _text; }

            set
            {
                _text = value;

                if (_material != null)
                {
                    _material.TextValue = value;
                }
            }
        }

        public Game.EnumMathFreakFont Font
        {
            get { return _fontEnum; }

            set
            {
                _fontEnum = value;

                if (_material != null)
                {
                    _material.SpriteFontName = Game.GetEnumeratedFontName(_fontEnum);
                }
            }
        }

        public Vector4 RGBA
        {
            get { return _rgba; }
            
            set
            {
                _rgba = value;

                if (_material != null)
                {
                    _material.TextColour = new Color(value);
                }
            }
        }

        public int TextHeight
        {
            get { return _textHeight; }

            set
            {
                _textHeight = value;

                if (_material != null)
                {
                    _material.TextHeight = _textHeight;
                }
            }
        }

        public int TextOffsetY
        {
            get { return _textOffsetY; }

            set
            {
                _textOffsetY = value;

                if (_material != null)
                {
                    _material.TextOffsetY = _textOffsetY;
                }
            }
        }

        public int Spacer
        {
            get { return _spacer; }

            set
            {
                _spacer = value;

                if (_material != null)
                {
                    _material.Spacer = _spacer;
                }
            }
        }

        public int IconHeight
        {
            get { return _iconHeight; }

            set
            {
                _iconHeight = value;

                if (_material != null)
                {
                    _material.IconHeight = _iconHeight;
                }
            }
        }

        public RenderMaterial Icon
        {
            get { return _icon; }

            set
            {
                _icon = value;

                if (_material != null)
                {
                    _material.Icon = (_icon as SimpleMaterial).Texture.Instance as Texture2D;
                }
            }
        }

        public bool TextOnRight
        {
            get { return _textOnRight; }

            set
            {
                _textOnRight = value;

                if (_material != null)
                {
                    _material.TextOnRight = _textOnRight;
                }
            }
        }

        public PromptMaterial.EnumAlign Align
        {
            get { return _align; }

            set
            {
                _align = value;

                if (_material != null)
                {
                    _material.Align = _align;
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
            if (!base._OnRegister(owner) || !(owner is T2DStaticSprite))
                return false;

            _sprite = (owner as T2DStaticSprite);

            // can't access the material's surfaceformat yet as that texture hasn't been set up, so use the alternate constructor
            _material = new PromptMaterial(_sprite.Size, SurfaceFormat.Color);

            _material.TextValue = _text;
            _material.SpriteFontName = Game.GetEnumeratedFontName(_fontEnum);
            _material.TextColour = new Color(_rgba);
            _material.TextHeight = _textHeight;
            _material.TextOffsetY = _textOffsetY;
            _material.Spacer = _spacer;
            _material.IconHeight = _iconHeight;
            _material.Icon = (_icon as SimpleMaterial).Texture.Instance as Texture2D;
            _material.TextOnRight = _textOnRight;
            _material.Align = _align;

            // replace the sprite's material with the TextMaterial
            _sprite.Material = _material;

            PreDrawManager.Instance.Register(_material);

            return true;
        }

        protected override void _OnUnregister()
        {
            PreDrawManager.Instance.UnRegister(_material);
            _material.Dispose();

            base._OnUnregister();
        }

        #endregion

        //======================================================
        #region Private, protected, internal fields

        private T2DStaticSprite _sprite;

        private String _text;
        private Game.EnumMathFreakFont _fontEnum;
        private Vector4 _rgba;
        private int _textHeight;
        private int _textOffsetY;
        private int _spacer;
        private RenderMaterial _icon;
        private int _iconHeight;
        private bool _textOnRight;
        private PromptMaterial.EnumAlign _align;

        protected PromptMaterial _material;

        #endregion
    }
}
