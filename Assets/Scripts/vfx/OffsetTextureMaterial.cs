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
    /// Allows partial drawing of a texture
    /// </summary>
    public class OffsetTextureMaterial : SimpleMaterial
    {
        //======================================================
        #region Constructors

        public OffsetTextureMaterial()
        {
            EffectFilename = "data/effects/OffsetTextureEffect";
        }

        #endregion

        //======================================================
        #region Public properties, operators, constants, and enums

        public Vector2 Offset;

        #endregion

        //======================================================
        #region Public methods
        #endregion

        //======================================================
        #region Private, protected, internal methods

        protected override void _SetupObjectParameters(RenderInstance renderInstance, SceneRenderState srs)
        {
            base._SetupObjectParameters(renderInstance, srs);

            EffectManager.SetParameter(_offsetParam, Offset);
        }

        protected override void _LoadParameters()
        {
            base._LoadParameters();

            _offsetParam = EffectManager.GetParameter(Effect, "offset");
        }

        protected override void _ClearParameters()
        {
            _offsetParam = null;

            base._ClearParameters();
        }

        #endregion

        //======================================================
        #region Private, protected, internal fields

        private EffectParameter _offsetParam;

        #endregion
    }
}
