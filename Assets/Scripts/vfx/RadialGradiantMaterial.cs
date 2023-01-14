using System;
using System.Collections.Generic;
using System.Text;

using GarageGames.Torque.Core;
using GarageGames.Torque.SceneGraph;
using GarageGames.Torque.RenderManager;

using Microsoft.Xna.Framework.Graphics;
using GarageGames.Torque.Materials;
using Microsoft.Xna.Framework;

namespace MathFreak.vfx
{
    /// <summary>
    /// Renders a radial gradient between two colours
    /// </summary>
    public class RadialGradiantMaterial : SimpleMaterial
    {
        //======================================================
        #region Constructors

        public RadialGradiantMaterial()
        {
            EffectFilename = "data/effects/RadialGradientEffect";
        }

        #endregion

        //======================================================
        #region Public properties, operators, constants, and enums

        public Vector4 InnerColor;
        public Vector4 OuterColor;

        #endregion

        //======================================================
        #region Public methods
        #endregion

        //======================================================
        #region Private, protected, internal methods

        protected override void _SetupObjectParameters(RenderInstance renderInstance, SceneRenderState srs)
        {
            base._SetupObjectParameters(renderInstance, srs);

            EffectManager.SetParameter(_innerColorParam, InnerColor);
            EffectManager.SetParameter(_outerColorParam, OuterColor);
        }

        protected override void _LoadParameters()
        {
            base._LoadParameters();

            _innerColorParam = EffectManager.GetParameter(Effect, "innerColor");
            _outerColorParam = EffectManager.GetParameter(Effect, "outerColor");
        }

        protected override void _ClearParameters()
        {
            _innerColorParam = null;
            _outerColorParam = null;

            base._ClearParameters();
        }

        #endregion

        //======================================================
        #region Private, protected, internal fields

        private EffectParameter _innerColorParam;
        private EffectParameter _outerColorParam;

        #endregion
    }
}
