using System;
using System.Diagnostics;
using System.Collections.Generic;
////using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using GarageGames.Torque.Core;
using GarageGames.Torque.Sim;
using GarageGames.Torque.T2D;
using GarageGames.Torque.Materials;

using MathFreak.Text;



namespace MathFreak.GUIFrameWork
{
    /// <summary>
    /// Custom button for the Math Freak menu buttons
    /// </summary>
    [TorqueXmlSchemaType]
    public class MFTextMenuButton : GUIBasicButton, ITickObject
    {
        private const float ANIMDURATION = 0.12f;
        private const float MAXSCALE = 1.5f;
        private const float SCALESTEP_PER_SECOND = (MAXSCALE - 1.0f) / ANIMDURATION;
        
        public String TextLabel
        {
            get { return _buttonLabel; }

            set
            {
                _buttonLabel = value;

                if (_focusMaterial != null)
                {
                    _focusMaterial.TextValue = value;
                }

                if (_idleMaterial != null)
                {
                    _idleMaterial.TextValue = value;
                }

                if (_disabledMaterial != null)
                {
                    _disabledMaterial.TextValue = value;
                }
            }
        }

        [TorqueXmlSchemaType(DefaultValue = "1")]
        public override bool Enabled
        {
            get
            {
                return base.Enabled;
            }

            set
            {
                base.Enabled = value;

                // can't set the materials stuff if nothing to set it on - duh
                if (_sprite != null)
                {
                    _sprite.Material = value ? _idleMaterial : _disabledMaterial;
                }
            }
        }

        public override void OnGainedFocus()
        {
            base.OnGainedFocus();

            _sprite.Material = _focusMaterial;
            _gainedFocus = true;
            _sprite.Layer = _layer - 1;
            _ticking = true;
        }

        public override void OnLostFocus()
        {
            base.OnLostFocus();

            _sprite.Material = _idleMaterial;
            _gainedFocus = false;
            _sprite.Layer = _layer;
            _ticking = true;
        }

        public void ProcessTick(Move move, float elapsed)
        {
            if (!_ticking) return;

            if (_gainedFocus)
            {
                _scale += SCALESTEP_PER_SECOND * elapsed;

                if (_scale > MAXSCALE)
                {
                    _scale = MAXSCALE;
                    _ticking = false;
                }
            }
            else
            {
                _scale -= SCALESTEP_PER_SECOND * elapsed;

                if (_scale < 1.0f)
                {
                    _scale = 1.0f;
                    _ticking = false;
                }
            }

            _sprite.Size = new Vector2(_baseSize.X * _scale, _baseSize.Y * _scale);
        }

        public void InterpolateTick(float k) { }

        protected override bool _OnRegister(TorqueObject owner)
        {
            if (!base._OnRegister(owner) || !(owner is T2DStaticSprite))
                return false;

            ProcessList.Instance.AddTickCallback(owner, this);
            _sprite = owner as T2DStaticSprite;

            _baseSize = _sprite.Size;
            _scale = 1.0f;

            _layer = SceneObject.Layer;

            // can't access the material's surfaceformat yet as that texture hasn't been set up, so use the alternate constructor
            _focusMaterial = new TextMaterial(_sprite.Size, (_sprite.Material as SimpleMaterial));
            _idleMaterial = new TextMaterial(_sprite.Size, (_sprite.Material as SimpleMaterial));
            _disabledMaterial = new TextMaterial(_sprite.Size, (_sprite.Material as SimpleMaterial));

            _focusMaterial.TextValue = _buttonLabel;
            _idleMaterial.TextValue = _buttonLabel;
            _disabledMaterial.TextValue = _buttonLabel;
            _focusMaterial.SpriteFontName = Game.GetEnumeratedFontName(Game.EnumMathFreakFont.MenuButton);
            _idleMaterial.SpriteFontName = Game.GetEnumeratedFontName(Game.EnumMathFreakFont.MenuButton);
            _disabledMaterial.SpriteFontName = Game.GetEnumeratedFontName(Game.EnumMathFreakFont.MenuButton);
            _focusMaterial.Align = TextMaterial.Alignment.centered;
            _idleMaterial.Align = TextMaterial.Alignment.centered;
            _disabledMaterial.Align = TextMaterial.Alignment.centered;
            _focusMaterial.Colour = new Color(255, 255, 255, 255);
            _idleMaterial.Colour = new Color(200, 200, 255, 255);
            _disabledMaterial.Colour = new Color(200, 200, 200, 196);

            PreDrawManager.Instance.Register(_focusMaterial);
            PreDrawManager.Instance.Register(_idleMaterial);
            PreDrawManager.Instance.Register(_disabledMaterial);

            _sprite.Material = Enabled ? _idleMaterial : _disabledMaterial;

            return true;
        }

        protected override void _OnUnregister()
        {
            PreDrawManager.Instance.UnRegister(_focusMaterial);
            PreDrawManager.Instance.UnRegister(_idleMaterial);
            PreDrawManager.Instance.UnRegister(_disabledMaterial);

            _focusMaterial.Dispose();
            _idleMaterial.Dispose();
            _disabledMaterial.Dispose();

            base._OnUnregister();
        }

        private T2DStaticSprite _sprite;

        private String _buttonLabel;
        private TextMaterial _focusMaterial;
        private TextMaterial _idleMaterial;
        private TextMaterial _disabledMaterial;

        private bool _gainedFocus;
        private Vector2 _baseSize;
        private float _scale;
        private bool _ticking;

        private int _layer;
    }
}
