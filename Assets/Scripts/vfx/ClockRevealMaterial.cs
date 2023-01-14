using System;
using System.Collections.Generic;
using System.Text;

using GarageGames.Torque.Core;
using GarageGames.Torque.SceneGraph;
using GarageGames.Torque.RenderManager;

using Microsoft.Xna.Framework.Graphics;
using GarageGames.Torque.Materials;

namespace MathFreak.vfx
{
    /// <summary>
    /// This material will render its texture in the manner of a clock face or 'pie' being
    /// gradually revealed or hidden.  That is, it will render a more or less of the texture depending
    /// upon the sweep angle specified.  So it can render anything from a narrow segment to the full
    /// texture (which would be a 360 degree sweep).
    /// 
    /// Extends from simple material and adds access to a SweepAngle parameter and uses a custom shader (effect).
    /// 
    /// Techniques
    /// 
    /// CopyTechnique: IsCopyPass true
    /// TexturedTechnique: Texture set and IsColorBlended false
    /// ColorTextureBlendTechnique: Texture set and IsColorBlended true
    /// ColoredTechnique: Texture not set
    /// </summary>
    public class ClockRevealMaterial : SimpleMaterial
    {
        //======================================================
        #region Constructors

        public ClockRevealMaterial()
        {
            EffectFilename = "data/effects/ClockRevealEffect";
        }

        #endregion

        //======================================================
        #region Public properties, operators, constants, and enums
        
        public float SweepAngleInRadiansAngle = 0.0f;

        #endregion

        //======================================================
        #region Public methods
        #endregion

        //======================================================
        #region Private, protected, internal methods

        protected override void _SetupObjectParameters(RenderInstance renderInstance, SceneRenderState srs)
        {
            base._SetupObjectParameters(renderInstance, srs);

            EffectManager.SetParameter(_sweepAngleInRadiansParam, SweepAngleInRadiansAngle);
        }

        protected override void _LoadParameters()
        {
            base._LoadParameters();

            _sweepAngleInRadiansParam = EffectManager.GetParameter(Effect, "sweepAngleInRadians");
        }

        protected override void _ClearParameters()
        {
            _sweepAngleInRadiansParam = null;

            base._ClearParameters();
        }

        #endregion

        //======================================================
        #region Private, protected, internal fields

        private EffectParameter _sweepAngleInRadiansParam;

        #endregion
    }
}
