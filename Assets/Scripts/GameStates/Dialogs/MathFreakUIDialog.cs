using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

using GarageGames.Torque.GameUtil;
using GarageGames.Torque.Core;
using GarageGames.Torque.Materials;
//using GarageGames.Torque.SceneGraph;
using GarageGames.Torque.Sim;
//using GarageGames.Torque.GameUtil;
using GarageGames.Torque.T2D;
using GarageGames.Torque.Platform;

using MathFreak.GameStateFramework;
using MathFreak.GUIFrameWork;
using System.Threading;
using MathFreak.AsyncTaskFramework;



namespace MathFreak.GameStates.Dialogs
{
    /// <summary>
    /// This is the base class for the UI dialogs' common functionality.
    /// </summary>
    public abstract class MathFreakUIDialog : GameStateDialog
    {
        public override void OnTransitionOnCompleted()
        {
            base.OnTransitionOnCompleted();

            MFSoundManager.Instance.PlaySFX(MFSoundManager.EnumSFX.DialogShown);

            GUIManager.Instance.ActivateGUI();
        }

        public override void PreTransitionOff()
        {
            base.PreTransitionOff();

            MFSoundManager.Instance.PlaySFX(MFSoundManager.EnumSFX.DialogHidden);

            GUIManager.Instance.DeactivateGUI();
        }
    }
}
