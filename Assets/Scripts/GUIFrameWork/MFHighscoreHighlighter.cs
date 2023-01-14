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
    /// Custom button for highlighting highscore entries
    /// 
    /// NOTE: this particular button is used slightly differently from a normal menu type button.
    /// It should not have any north/south neighbours defined.  It will call back to the highscores
    /// screen when up/down movement is recieved and the highscore list will determine the shift
    /// of focus from one button to the next.
    /// </summary>
    [TorqueXmlSchemaType]
    public class MFHighscoreHighlighter : GUIBasicButton, ITickObject
    {
        private const float ANIMDURATION = 0.05f;
        private const float IDLE_ALPHA = 0.0f;
        private const float INCSTEP_PER_SECOND = (1.0f - IDLE_ALPHA) / ANIMDURATION;

        private T2DStaticSprite _sprite;
        private bool _gainedFocus;
        private bool _ticking;
        private GUIActionDelegate _action2; // the action that happens when the player presses downward on the gamepad (the parent class' 'action' will be used when the player presses up on the gamepad)

        public GUIActionDelegate Action2
        {
            get { return _action2; }
            set { _action2 = value; }
        }


        public override void  OnMoveUp()
        {
            MFSoundManager.Instance.PlaySFX(MFSoundManager.EnumSFX.ButtonPressed);

            GUIManager.Instance.OnActionStarted();

            if (Action != null) Action(this, Params);

            GUIManager.Instance.OnActionCompleted();
        }

        public override void OnMoveDown()
        {
            MFSoundManager.Instance.PlaySFX(MFSoundManager.EnumSFX.ButtonPressed);

            GUIManager.Instance.OnActionStarted();

            if (Action2 != null) Action2(this, Params);

            GUIManager.Instance.OnActionCompleted();
        }

        public override void OnClicked()
        {
            // do nothing - we want to disable the default click behaviour
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
    }
}
