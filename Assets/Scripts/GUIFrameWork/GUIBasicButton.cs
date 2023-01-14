using System;
using System.Collections.Generic;
//////using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;

using GarageGames.Torque.Core;
using GarageGames.Torque.T2D;



namespace MathFreak.GUIFrameWork
{
    /// <summary>
    /// Base class for clickable buttons - doesn't actually do any visual changes when clicked; just behavioural functionality
    /// </summary>
    [TorqueXmlSchemaType]
    public class GUIBasicButton : GUIComponent
    {
        public GUIActionDelegate Action
        {
            get { return _action; }
            set { _action = value; }
        }

        public string Params
        {
            get { return _params; }
            set { _params = value; }
        }

        public override void OnClicked()
        {
            MFSoundManager.Instance.PlaySFX(MFSoundManager.EnumSFX.ButtonPressed);

            GUIManager.Instance.OnActionStarted();

            if (_action != null) _action(this, _params);

            GUIManager.Instance.OnActionCompleted();
        }

        public override void OnGainedFocus()
        {
            base.OnGainedFocus();

            MFSoundManager.Instance.PlaySFX(MFSoundManager.EnumSFX.ButtonHiglight);
        }

        private GUIActionDelegate _action;
        private string _params;
    }
}
