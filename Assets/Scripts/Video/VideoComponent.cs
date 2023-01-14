using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

using GarageGames.Torque.Core;
using GarageGames.Torque.Util;
using GarageGames.Torque.Sim;
using GarageGames.Torque.T2D;
using GarageGames.Torque.SceneGraph;
using GarageGames.Torque.MathUtil;
using GarageGames.Torque.GUI;
using GarageGames.Torque.Materials;
using GarageGames.Torque.XNA;
using GarageGames.Torque.GFX;
using MathFreak.ContentLoading;
using Microsoft.Xna.Framework.Media;



namespace MathFreak.Text
{
    /// <summary>
    /// This component allows assigning a wmv video file to be played on T2DStaticSprites
    /// instead of the usual static texture.  NOTE: the current implementation does not
    /// support changing the video at runtime, but this would be an easy feature add.
    /// </summary>
    [TorqueXmlSchemaType]
    public class VideoComponent : TorqueComponent
    {
        //======================================================
        #region Static methods, fields, constructors
        #endregion

        //======================================================
        #region Constructors
        #endregion

        //======================================================
        #region Public properties, operators, constants, and enums

        public string VideoFileName
        {
            get { return _videoFileName; }
            set { _videoFileName = value; }
        }

        public MediaState VideoPlayerState
        {
            get { return _material.Player.State; }
        }

        #endregion

        //======================================================
        #region Public methods

        public override void CopyTo(TorqueComponent obj)
        {
            base.CopyTo(obj);

            VideoComponent obj2 = obj as VideoComponent;

            obj2.VideoFileName = VideoFileName;
        }

        public void Play()
        {
            _material.Player.Play(_video);
        }

        public void Stop()
        {
            _material.Player.Stop();
        }

        #endregion

        //======================================================
        #region Private, protected, internal methods

        protected override bool _OnRegister(TorqueObject owner)
        {
            Assert.Fatal(owner is T2DStaticSprite, "VideoComponent can only be assigned to a T2DStaticSprite");

            if (!base._OnRegister(owner) || !(owner is T2DStaticSprite))
                return false;

            _sprite = (owner as T2DStaticSprite);

            // create the video material and load the video asset
            _material = new VideoMaterial();
            _video = (ResourceManager.Instance.CurrentContentManager as ContentLoader).Load<Video>(_videoFileName);
            //_material.Player.Volume = 0.0f;

            // replace the sprite's material with the VideoMaterial
            _sprite.Material = _material;

            // register the video material for updating every frame
            PreDrawManager.Instance.Register(_material);

            return true;
        }

        protected override void _OnUnregister()
        {
            PreDrawManager.Instance.UnRegister(_material);
            _material.Dispose();
            base._OnUnregister();
        }

        #endregion

        //======================================================
        #region Private, protected, internal fields

        private T2DStaticSprite _sprite;

        private String _videoFileName;
        private VideoMaterial _material;
        private Video _video;

        #endregion
    }
}
