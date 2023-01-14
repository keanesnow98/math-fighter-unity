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
using Microsoft.Xna.Framework.Net;
using System.Xml.Serialization;



namespace MathFreak.GUIFrameWork
{
    /// <summary>
    /// Custom button for the Math Freak available match button.
    /// </summary>
    [TorqueXmlSchemaType]
    public class MFAvailableMatchButton : GUIBasicButton, ITickObject
    {
        private const float ANIMDURATION = 0.3f;
        private const float IDLE_ALPHA = 0.3f;
        private const float INCSTEP_PER_SECOND = (1.0f - IDLE_ALPHA) / ANIMDURATION;

        [XmlIgnore]
        public int SessionIndex
        {
            get { return _sessionIndex; }
            set { _sessionIndex = value; }
        }

        public override void OnGainedFocus()
        {
            base.OnGainedFocus();

            _gainedFocus = true;
            _ticking = true;
        }

        public override void OnLostFocus()
        {
            base.OnLostFocus();

            _gainedFocus = false;
            _ticking = true;
        }

        public void ProcessTick(Move move, float elapsed)
        {
            if (!_ticking) return;

            if (_gainedFocus)
            {
                _sprite.VisibilityLevel += INCSTEP_PER_SECOND * elapsed;

                if (_sprite.VisibilityLevel >= 1.0f)
                {
                    _sprite.VisibilityLevel = 1.0f;
                    _ticking = false;
                }
            }
            else
            {
                _sprite.VisibilityLevel -= INCSTEP_PER_SECOND * elapsed;

                if (_sprite.VisibilityLevel <= IDLE_ALPHA)
                {
                    _sprite.VisibilityLevel = IDLE_ALPHA;
                    _ticking = false;
                }
            }
        }

        public void InterpolateTick(float k) { }

        protected override bool _OnRegister(TorqueObject owner)
        {
            if (!base._OnRegister(owner) || !(owner is T2DStaticSprite))
                return false;

            ProcessList.Instance.AddTickCallback(owner, this);
            _sprite = owner as T2DStaticSprite;

            // set up the sprite so it can have transparency levels applied
            _sprite.Material.IsTranslucent = true;
            (_sprite.Material as SimpleMaterial).IsColorBlended = true;

            _sprite.VisibilityLevel = IDLE_ALPHA;

            return true;
        }

        private T2DStaticSprite _sprite;

        private bool _gainedFocus;
        private bool _ticking;

        private int _sessionIndex;
    }
}
