using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GarageGames.Torque.T2D;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GarageGames.Torque.Core;
using GarageGames.Torque.XNA;

namespace MathFreak.Math.Views
{
    /// <summary>
    /// Base class for the view of a math element
    /// </summary>
    public abstract class MathElementView
    {
        //protected static Vector4 _backgroundColor;
        protected LogicalRenderTexture _texture;

        public LogicalRenderTexture Texture
        {
            get { return _texture; }
        }

        //// call this to setup render settings such as the surface format that will be used
        //public static void InitializeRenderSettings(Vector4 backgroundColor)
        //{
        //    _backgroundColor = backgroundColor;
        //}
        
        public static Vector3 GetTextSize(String text, float h, SpriteFont font)
        {
            Assert.Fatal(text != null && text.Length != 0, "Trying to create a math expression element view without any content");

            Vector2 textSize = font.MeasureString(text);

            float textScale = h / textSize.Y;

            return new Vector3(textSize.X * textScale, textSize.Y * textScale, textScale);
        }

        // rendering one element at a time is not the most efficient way to do this stuff, but
        // we only need to generate the math expression visual representation once and it
        // won't change after that, so we'll just keep things simple.
        public static LogicalRenderTexture DrawStringToTexture(string text, float h, SpriteFont font)
        {
            // work out how much space the text will take up when rendered
            Vector3 textSize = GetTextSize(text, h, font);
            
            // create a render target
            LogicalRenderTexture lrt = LRTPool.Instance.AcquireLRT((int)textSize.X, (int)textSize.Y);

            // setup the device stuff
            TorqueEngineComponent.Instance.Game.GraphicsDevice.SetRenderTarget(0, lrt.RenderTarget);
            TorqueEngineComponent.Instance.Game.GraphicsDevice.Clear(ClearOptions.Target, Color.TransparentBlack, 0.0f, 0);

            // setup the spritebatch
            SpriteBatch spriteBatch = Game.SpriteBatch;// new SpriteBatch(TorqueEngineComponent.Instance.Game.GraphicsDevice);

            // render text to texture
            spriteBatch.Begin();
            spriteBatch.DrawString(font, text, Vector2.Zero, Color.White, 0.0f, Vector2.Zero, textSize.Z, SpriteEffects.None, 0.0f);
            spriteBatch.End();

            // reset device stuff
            TorqueEngineComponent.Instance.Game.GraphicsDevice.SetRenderTarget(0, null);

            // resolve and return the newly rendered texture
            lrt.ResolveTexture();
            return lrt;
        }
    }
}
