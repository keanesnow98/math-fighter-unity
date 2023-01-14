using System;
using System.Diagnostics;
using System.Collections.Generic;
////using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;

using GarageGames.Torque.Core;
using GarageGames.Torque.Sim;
using GarageGames.Torque.T2D;
using GarageGames.Torque.Materials;



namespace MathFreak.GUIFrameWork
{
    /// <summary>
    /// Used to be the custom button for the Math Freak main menu buttons.  Now just a generic
    /// grow/shrink image button implementation.
    /// </summary>
    [TorqueXmlSchemaType]
    public class MFImageButton : GUIBasicButton, ITickObject
    {
        private const float ANIMDURATION = 0.12f;
        private const float MAXSCALE = 1.5f;
        private const float SCALESTEP_PER_SECOND = (MAXSCALE - 1.0f) / ANIMDURATION;

        public RenderMaterial FocusMaterial
        {
            get { return _focusMaterial; }
            set { _focusMaterial = value; }
        }

        public int FocusMaterialIndex
        {
            get { return _focusMaterialIndex; }
            set { _focusMaterialIndex = value; }
        }

        public RenderMaterial IdleMaterial
        {
            get { return _idleMaterial; }
            set { _idleMaterial = value; }
        }

        public int IdleMaterialIndex
        {
            get { return _idleMaterialIndex; }
            set { _idleMaterialIndex = value; }
        }

        public override void OnGainedFocus()
        {
            base.OnGainedFocus();

            _sprite.Material = _focusMaterial;
            _sprite.MaterialRegionIndex = _focusMaterialIndex;
            _sprite.Layer = _layer - 1;
            _gainedFocus = true;
            _ticking = true;
        }

        public override void OnLostFocus()
        {
            base.OnLostFocus();

            _sprite.Material = _idleMaterial;
            _sprite.MaterialRegionIndex = _idleMaterialIndex;
            _sprite.Layer = _layer;
            _gainedFocus = false;
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

            return true;
        }

        private T2DStaticSprite _sprite;
        private RenderMaterial _focusMaterial;
        private int _focusMaterialIndex;
        private RenderMaterial _idleMaterial;
        private int _idleMaterialIndex;

        private bool _gainedFocus;
        private Vector2 _baseSize;
        private float _scale;
        private bool _ticking;

        private int _layer;
    }
}
