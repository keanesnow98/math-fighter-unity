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



namespace MathFreak.Text
{
    /// <summary>
    /// Okay, so bottom line... XNA text handling pretty much sucks and Torque X (a.k.a. TX) doesn't do anything
    /// to improve on it.  In particular, if you want to render text at different layers in a scene
    /// then you're out of luck with TX.
    /// 
    /// This material allows you to have text rendered onto any object that can use a SimpleMaterial
    /// for applying it's texture.  From there on the object can be treated as any other object would be, including
    /// setting the layer, rotating, moving, physics, or whatever.
    /// 
    /// NOTE: you need to register any instances of TextMaterial with the PreDrawManager or the text will
    /// not get rendered (or updated if you change the text string).  You should also unregister the texture
    /// when it is no longer required - registering/unregistering is not done automatically as there is no way
    /// the texture can know when it should unregister itself.
    /// </summary>
    public class TextMaterial : SimpleMaterial, IPreDraw
    {
        private String _text;
        private String _fontName;
        private Alignment _align;
        private Color _colour;

        private bool _invalidatedFont;
        private bool _invalidated;
        private Resource<SpriteFont> _spriteFont;
        private RenderTarget2D _myTarget;
        private Vector2 _textureSize;
        private SurfaceFormat _format;
        private SimpleMaterial _referenceMaterial;

        public enum Alignment { left, right, centered };

        public String TextValue
        {
            get { return _text; }

            set
            {
                if (_text != value)
                {
                    _invalidated = true;
                    _text = value;
                }

                if (_text == null)
                {
                    _text = "";
                }
            }
        }

        public String SpriteFontName
        {
            get { return _fontName; }

            set
            {
                if (_fontName != value)
                {
                    _invalidatedFont = true;
                    _fontName = value;
                }
            }
        }

        public Alignment Align
        {
            get { return _align; }

            set
            {
                if (_align != value)
                {
                    _invalidated = true;
                    _align = value;
                }
            }
        }

        public Color Colour
        {
            get { return _colour; }

            set
            {
                if (_colour != value)
                {
                    _invalidated = true;
                    _colour = value;
                }
            }
        }


        public TextMaterial(Vector2 size, SurfaceFormat format)
        {
            _textureSize = size;
            _format = format;
            _invalidated = true;
            _invalidatedFont = true;
            _text = "";

            TorqueEventManager.ListenEvents<bool>(GFXDevice.Instance.DeviceReset, OnDeviceReset);
        }

        /// <summary>
        /// Alternate constructor
        /// </summary>
        /// <param name="size">Size of texture to create</param>
        /// <param name="referenceMaterial">Reference to a material to copy the surfaceformat of</param>
        public TextMaterial(Vector2 size, SimpleMaterial referenceMaterial)
        {
            _textureSize = size;
            _referenceMaterial = referenceMaterial;
            _format = SurfaceFormat.Unknown;
            _invalidated = true;
            _invalidatedFont = true;
            _text = "";

            TorqueEventManager.ListenEvents<bool>(GFXDevice.Instance.DeviceReset, OnDeviceReset);
        }

        public void OnDeviceReset(String eventName, bool blah)
        {
            // graphics device has been reset - we will need to re-get the sprite font and re-render the text to the texture
            _invalidatedFont = true;
        }

        public void PreDraw(float dt)
        {
            // get font?
            if (_invalidatedFont)
            {
                _invalidatedFont = false;
                _invalidated = true;
                _spriteFont = ResourceManager.Instance.LoadFont(@"data/fonts/" + _fontName);
            }

            // update texture?
            if (_invalidated)
            {
                _invalidated = false;
                Texture2D texture = DrawTextToTexture();
                SetTexture(texture);
            }
        }

        public Vector3 GetTextSize(String text)
        {
            // workout how much the text needs scaling by (scale it so it matches the height of the sprite)
            //if (_invalidatedFont)
            //{
            //    _invalidatedFont = false;
            //    _invalidated = true;
            //    _spriteFont = ResourceManager.Instance.LoadFont(@"data/fonts/" + _fontName);
            //}

            Vector2 textSize;

            if (text == null || text.Length == 0)
            {
                textSize = _spriteFont.Instance.MeasureString("A");
                textSize.X = 0.0f;
            }
            else
            {
                textSize = _spriteFont.Instance.MeasureString(_text);
            }

            float textScale = _textureSize.Y / textSize.Y;

            return new Vector3(textSize.X * textScale, textSize.Y * textScale, textScale);
        }

        private Texture2D DrawTextToTexture()
        {
            Vector3 textSize = GetTextSize(_text);

            // calculate where to position the text based on it's size and the specified alignment
            Vector2 start;

            switch (_align)
            {
                case Alignment.right:
                    start = new Vector2(_textureSize.X - textSize.X, 0.0f);
                    break;

                case Alignment.centered:
                    start = new Vector2((_textureSize.X - textSize.X) / 2.0f, 0.0f);
                    break;

                default:
                    start = new Vector2(0.0f, 0.0f);
                    break;
            }

            // do the rendering stuff

            // create a render target if we haven't already
            if (_myTarget == null)
            {
                if (_format == SurfaceFormat.Unknown)
                {
                    _format = (_referenceMaterial.Texture.Instance as Texture2D).Format;
                }

                _myTarget = Util.CreateRenderTarget2D(Game.Instance.GraphicsDevice, (int)_textureSize.X, (int)_textureSize.Y, 1, _format);// new RenderTarget2D(Game.Instance.GraphicsDevice, (int)_textureSize.X, (int)_textureSize.Y, 1, _format);
            }

            // setup the device stuff
            TorqueEngineComponent.Instance.Game.GraphicsDevice.SetRenderTarget(0, _myTarget);
            TorqueEngineComponent.Instance.Game.GraphicsDevice.Clear(ClearOptions.Target, new Vector4(0f, 0f, 0f, 0f), 0.0f, 0);

            // setup the spritebatch
            SpriteBatch spriteBatch = Game.SpriteBatch;// new SpriteBatch(TorqueEngineComponent.Instance.Game.GraphicsDevice);

            // render text to texture
            spriteBatch.Begin();
            spriteBatch.DrawString(_spriteFont.Instance, _text, start, _colour, 0.0f, Vector2.Zero, textSize.Z, SpriteEffects.None, 0.0f);
            spriteBatch.End();

            // reset device stuff
            TorqueEngineComponent.Instance.Game.GraphicsDevice.SetRenderTarget(0, null);

            // return the newly rendered texture
            return _myTarget.GetTexture();
        }

        public override void Dispose()
        {
            if (!Texture.IsNull)
            {
                Texture.Instance.Dispose();
            }

            if (_myTarget != null)
            {
                _myTarget.Dispose();
            }

            // not listening to events anymore so remove the listener or we'll not get garbage collected
            TorqueEventManager.SilenceEvents<bool>(GFXDevice.Instance.DeviceReset, OnDeviceReset);

            base.Dispose();
        }
    }
}
