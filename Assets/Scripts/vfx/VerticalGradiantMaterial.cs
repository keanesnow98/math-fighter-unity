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
    /// Renders a vertical gradient between two colours
    /// </summary>
    public class VerticalGradiantMaterial : SimpleMaterial
    {
        //======================================================
        #region Constructors

        public VerticalGradiantMaterial()
        {
            EffectFilename = "data/effects/VerticalGradientEffect";
        }

        #endregion

        //======================================================
        #region Public properties, operators, constants, and enums

        public Vector4 BottomColor;
        public Vector4 TopColor;

        #endregion

        //======================================================
        #region Public methods
        #endregion

        //======================================================
        #region Private, protected, internal methods

        protected override void _SetupObjectParameters(RenderInstance renderInstance, SceneRenderState srs)
        {
            base._SetupObjectParameters(renderInstance, srs);

            EffectManager.SetParameter(_bottomColorParam, BottomColor);
            EffectManager.SetParameter(_topColorParam, TopColor);
        }

        protected override void _LoadParameters()
        {
            base._LoadParameters();

            _bottomColorParam = EffectManager.GetParameter(Effect, "bottomColor");
            _topColorParam = EffectManager.GetParameter(Effect, "topColor");
        }

        protected override void _ClearParameters()
        {
            _bottomColorParam = null;
            _topColorParam = null;

            base._ClearParameters();
        }

        #endregion

        //======================================================
        #region Private, protected, internal fields

        private EffectParameter _bottomColorParam;
        private EffectParameter _topColorParam;

        #endregion
    }
}
