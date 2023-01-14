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
    /// Custom button for selecting an option using left/right movement - set the actions for the button
    /// to handle the left/right inputs.
    /// </summary>
    [TorqueXmlSchemaType]
    public class MFTextOptionButton : MFTextMenuButton, ITickObject
    {
        private GUIActionDelegate _action2; // the action that happens when the player presses rightward on the gamepad (the parent class' 'action' will be used when the player presses left on the gamepad)

        public GUIActionDelegate Action2
        {
            get { return _action2; }
            set { _action2 = value; }
        }


        public override void OnMoveLeft()
        {
            MFSoundManager.Instance.PlaySFX(MFSoundManager.EnumSFX.ButtonPressed);

            GUIManager.Instance.OnActionStarted();

            if (Action != null) Action(this, Params);

            GUIManager.Instance.OnActionCompleted();
        }

        public override void OnMoveRight()
        {
            MFSoundManager.Instance.PlaySFX(MFSoundManager.EnumSFX.ButtonPressed);

            GUIManager.Instance.OnActionStarted();

            if (_action2 != null) _action2(this, Params);

            GUIManager.Instance.OnActionCompleted();
        }

        public override void OnClicked()
        {
            // Overriding the baseclass so that nothing happens when the user clicks on this button.
            // (left/right on the gamepad is used to operate this kind of button instead)
        }
    }
}
