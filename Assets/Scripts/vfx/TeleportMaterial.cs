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
    public class TeleportMaterial : SimpleMaterial
    {
        //======================================================
        #region Constructors

        public TeleportMaterial()
        {
            EffectFilename = "data/effects/TeleportEffect";
        }

        #endregion

        //======================================================
        #region Public properties, operators, constants, and enums
        
        public float AnimTime = 0.0f;
        public Vector2 TeleportOffset = Vector2.Zero; // set this to random values to create a teleport effect
        public Texture TeleportTexture;

        #endregion

        //======================================================
        #region Public methods
        #endregion

        //======================================================
        #region Private, protected, internal methods

        protected override void _SetupObjectParameters(RenderInstance renderInstance, SceneRenderState srs)
        {
            base._SetupObjectParameters(renderInstance, srs);

            EffectManager.SetParameter(_animTimeParam, AnimTime);
            EffectManager.SetParameter(_teleportOffsetParam, TeleportOffset);
            EffectManager.SetParameter(_teleportTextureParameter, TeleportTexture);
        }

        protected override void _LoadParameters()
        {
            base._LoadParameters();

            _animTimeParam = EffectManager.GetParameter(Effect, "animTime");
            _teleportOffsetParam = EffectManager.GetParameter(Effect, "teleportOffset");
            _teleportTextureParameter = EffectManager.GetParameter(Effect, "teleportTexture");
        }

        protected override void _ClearParameters()
        {
            _animTimeParam = null;
            _teleportOffsetParam = null;
            _teleportTextureParameter = null;

            base._ClearParameters();
        }

        #endregion

        //======================================================
        #region Private, protected, internal fields

        private EffectParameter _animTimeParam;
        private EffectParameter _teleportOffsetParam;
        private EffectParameter _teleportTextureParameter;

        #endregion
    }
}
