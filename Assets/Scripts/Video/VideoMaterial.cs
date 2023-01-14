using System;
using System.Collections.Generic;
//using System.Linq;
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
using Microsoft.Xna.Framework.Media;
using System.Xml.Serialization;
using MathFreak.ContentLoading;



namespace MathFreak.Text
{
    /// <summary>
    /// Wraps up the low level aspects of video rendering in a nice and easy to use material.
    /// </summary>
    public class VideoMaterial : SimpleMaterial, IPreDraw
    {
        private VideoPlayer _player;


        public VideoPlayer Player
        {
            get { return _player; }
        }


        public VideoMaterial()
        {
            _player = new VideoPlayer();
        }

        public void PreDraw(float dt)
        {
            // update our texture from the video
            if (_player.State == MediaState.Playing)
            {
                SetTexture(_player.GetTexture());
            }
        }

        public override void Dispose()
        {
            _player.Stop();
            _player.Dispose();

            base.Dispose();
        }
    }
}
