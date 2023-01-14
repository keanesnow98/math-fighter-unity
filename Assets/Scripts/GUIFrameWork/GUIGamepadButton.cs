using System;
using System.Collections.Generic;
////using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

using GarageGames.Torque.Core;
using GarageGames.Torque.T2D;
using System.Diagnostics;



namespace MathFreak.GUIFrameWork
{
    /// <summary>
    /// This is for the kind of interface where the player needs to press a gamepad button instead
    /// of visually selecting a GUI item.  E.g. 'B' for going 'back' to a previous screen is a common
    /// gamepad button press on Xbox 360.
    /// 
    /// This component may be set as either visible or invisible via the StartsVisible property.
    /// </summary>
    [TorqueXmlSchemaType]
    public class GUIGamepadButton : GUIComponent
    {
        // enumerate the buttons so we see the button names in the editor instead of just the raw numbers
        public enum EnumGamepadButton {
            A = GUIInputComponent.BUTTON_A,
            B = GUIInputComponent.BUTTON_B,
            X = GUIInputComponent.BUTTON_X,
            Y = GUIInputComponent.BUTTON_Y,
            BACK = GUIInputComponent.BUTTON_BACK,
            START = GUIInputComponent.BUTTON_START,
            LSHOULDER = GUIInputComponent.BUTTON_LSHOULDER,
            RSHOULDER = GUIInputComponent.BUTTON_RSHOULDER,
            LTRIGGER = GUIInputComponent.BUTTON_LTRIGGER,
            RTRIGGER = GUIInputComponent.BUTTON_RTRIGGER,
        };

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

        public EnumGamepadButton Button
        {
            get { return _button; }
            set { _button = value; }
        }

        public bool StartsVisible
        {
            get { return _startsVisible; }
            set { _startsVisible = value; }
        }

        public override int OnProcessButtons(GarageGames.Torque.Util.ReadOnlyArray<GarageGames.Torque.Sim.MoveButton> buttons, int prevButton)
        {
            // if was already previously pressed then ignore it (player has to release a button and press it again to do the action again)
            if ((int)_button == prevButton) return -1;

            // else we check if our button is being pushed act accordingly
            if (buttons[(int)_button].Pushed)
            {
                GUIManager.Instance.OnActionStarted();

                if (_action != null) _action(this, _params);

                GUIManager.Instance.OnActionCompleted();

                return (int)_button;
            }
            else
            {
                return -1;
            }
        }

        public override bool HitTest(float x, float y)
        {
            return false;
        }

        protected override bool _OnRegister(TorqueObject owner)
        {
            if (!base._OnRegister(owner) || !(owner is T2DSceneObject))
                return false;

            SceneObject.Visible = _startsVisible;

            return true;
        }

        private GUIActionDelegate _action;
        private string _params;
        private bool _startsVisible;
        private EnumGamepadButton _button;
    }
}
