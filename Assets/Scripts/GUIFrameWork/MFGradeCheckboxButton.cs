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
using GarageGames.Torque.GFX;



namespace MathFreak.GUIFrameWork
{
    /// <summary>
    /// Custom button for the Math Freak grade checkboxes
    /// </summary>
    [TorqueXmlSchemaType]
    public class MFGradeCheckboxButton : GUIBasicButton, ITickObject
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
            }
        }

        public GUIActionDelegate GainedFocus
        {
            get { return _onGainedFocus; }
            set { _onGainedFocus = value; }
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

        public int CheckboxHeight
        {
            get { return _checkboxHeight; }
            set { _checkboxHeight = value; }
        }

        public int TextHeight
        {
            get { return _textHeight; }
            set { _textHeight = value; }
        }

        public int TextOffsetX
        {
            get { return _textOffsetX; }
            set { _textOffsetX = value; }
        }

        public int TextOffsetY
        {
            get { return _textOffsetY; }
            set { _textOffsetY = value; }
        }

        public RenderMaterial CheckboxEmpty
        {
            get { return _checkboxEmpty; }
            set { _checkboxEmpty = value; }
        }

        public RenderMaterial CheckboxTicked
        {
            get { return _checkboxTicked; }
            set { _checkboxTicked = value; }
        }

        public bool IsChecked
        {
            get { return _isChecked; }

            set
            {
                _isChecked = value;

                if (_focusMaterial != null)
                {
                    _focusMaterial.IsChecked = _isChecked;
                    _idleMaterial.IsChecked = _isChecked;
                    _disabledMaterial.IsChecked = _isChecked;
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

            if (_onGainedFocus != null) _onGainedFocus(this, Params);
        }

        public override void OnLostFocus()
        {
            base.OnLostFocus();

            _sprite.Material = _idleMaterial;
            _gainedFocus = false;
            _sprite.Layer = _layer;
            _ticking = true;
        }

        public override void OnClicked()
        {
            // do nothing - not responding to click
        }

        public override int OnProcessButtons(GarageGames.Torque.Util.ReadOnlyArray<MoveButton> buttons, int prevButton)
        {
            if (prevButton == GUIInputComponent.BUTTON_X) return -1;

            // else we check if our button is being pushed act accordingly
            if (buttons[GUIInputComponent.BUTTON_X].Pushed)
            {
                IsChecked = !IsChecked;
                base.OnClicked();
                return GUIInputComponent.BUTTON_X;
            }
            else
            {
                return -1;
            }
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

            _focusMaterial = new CheckboxMaterial(_sprite.Size, SurfaceFormat.Color);
            _idleMaterial = new CheckboxMaterial(_sprite.Size, SurfaceFormat.Color);
            _disabledMaterial = new CheckboxMaterial(_sprite.Size, SurfaceFormat.Color);

            _focusMaterial.TextValue = _buttonLabel;
            _idleMaterial.TextValue = _buttonLabel;
            _disabledMaterial.TextValue = _buttonLabel;
            _focusMaterial.SpriteFontName = Game.GetEnumeratedFontName(Game.EnumMathFreakFont.PlayerName);
            _idleMaterial.SpriteFontName = Game.GetEnumeratedFontName(Game.EnumMathFreakFont.PlayerName);
            _disabledMaterial.SpriteFontName = Game.GetEnumeratedFontName(Game.EnumMathFreakFont.PlayerName);
            _focusMaterial.TextColour = new Color(255, 255, 255, 255);
            _idleMaterial.TextColour = new Color(200, 200, 255, 255);
            _disabledMaterial.TextColour = new Color(200, 200, 200, 255);
            _focusMaterial.CheckboxEmpty = (_checkboxEmpty as SimpleMaterial).Texture.Instance as Texture2D;
            _idleMaterial.CheckboxEmpty = (_checkboxEmpty as SimpleMaterial).Texture.Instance as Texture2D;
            _disabledMaterial.CheckboxEmpty = (_checkboxEmpty as SimpleMaterial).Texture.Instance as Texture2D;
            _focusMaterial.CheckboxTicked = (_checkboxTicked as SimpleMaterial).Texture.Instance as Texture2D;
            _idleMaterial.CheckboxTicked = (_checkboxTicked as SimpleMaterial).Texture.Instance as Texture2D;
            _disabledMaterial.CheckboxTicked = (_checkboxTicked as SimpleMaterial).Texture.Instance as Texture2D;
            _focusMaterial.IsChecked = _isChecked;
            _idleMaterial.IsChecked = _isChecked;
            _disabledMaterial.IsChecked = _isChecked;
            _focusMaterial.TextHeight = _textHeight;
            _idleMaterial.TextHeight = _textHeight;
            _disabledMaterial.TextHeight = _textHeight;
            _focusMaterial.CheckboxHeight = _checkboxHeight;
            _idleMaterial.CheckboxHeight = _checkboxHeight;
            _disabledMaterial.CheckboxHeight = _checkboxHeight;
            _focusMaterial.TextOffsetX = _textOffsetX;
            _idleMaterial.TextOffsetX = _textOffsetX;
            _disabledMaterial.TextOffsetX = _textOffsetX;
            _focusMaterial.TextOffsetY = _textOffsetY;
            _idleMaterial.TextOffsetY = _textOffsetY;
            _disabledMaterial.TextOffsetY = _textOffsetY;

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
        private GUIActionDelegate _onGainedFocus;
        private CheckboxMaterial _focusMaterial;
        private CheckboxMaterial _idleMaterial;
        private CheckboxMaterial _disabledMaterial;

        private int _checkboxHeight;
        private int _textHeight;
        private int _textOffsetX;
        private int _textOffsetY;
        private RenderMaterial _checkboxEmpty;
        private RenderMaterial _checkboxTicked;
        private bool _isChecked;

        private bool _gainedFocus;
        private Vector2 _baseSize;
        private float _scale;
        private bool _ticking;

        private int _layer;
    }
}
