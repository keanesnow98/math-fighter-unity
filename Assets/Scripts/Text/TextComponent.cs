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
    /// This component allows you set T2DStaticSprites to having text rendered onto them instead of
    /// a normal bitmap material.
    /// </summary>
    [TorqueXmlSchemaType]
    public class TextComponent : TorqueComponent
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

        public TextMaterial.Alignment Align
        {
            get { return _align; }
            set
            {
                _align = value;

                if (_material != null)
                {
                    _material.Align = value;
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
                    _material.Colour = new Color(value);
                }
            }
        }

        #endregion

        //======================================================
        #region Public methods

        public override void CopyTo(TorqueComponent obj)
        {
            base.CopyTo(obj);

            TextComponent obj2 = obj as TextComponent;

            obj2.TextValue = TextValue;
            obj2.RGBA = RGBA;
            obj2.Align = Align;
            obj2.Font = Font;
        }

        #endregion

        //======================================================
        #region Private, protected, internal methods

        protected override bool _OnRegister(TorqueObject owner)
        {
            if (!base._OnRegister(owner) || !(owner is T2DStaticSprite))
                return false;

            _sprite = (owner as T2DStaticSprite);

            // can't access the material's surfaceformat yet as that texture hasn't been set up, so use the alternate constructor
            _material = new TextMaterial(_sprite.Size, (_sprite.Material as SimpleMaterial));

            _material.TextValue = _text;
            _material.SpriteFontName = Game.GetEnumeratedFontName(_fontEnum);
            _material.Align = _align;
            _material.Colour = new Color(_rgba);

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
        private TextMaterial.Alignment _align;
        private Vector4 _rgba;
        protected TextMaterial _material;

        #endregion
    }
}
