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
using System.Diagnostics;



namespace MathFreak.GUIFrameWork
{
    public class CheckboxMaterial : SimpleMaterial, IPreDraw
    {
        private String _text;
        private String _fontName;
        private Color _textColour;
        private int _textHeight;
        private int _textOffsetX;
        private int _textOffsetY;
        private int _checkboxHeight;
        private Texture2D _checkboxEmpty;
        private Texture2D _checkboxTicked;

        private bool _invalidatedFont;
        private bool _invalidated;
        private Resource<SpriteFont> _spriteFont;
        private RenderTarget2D _myTarget;
        private Vector2 _textureSize;
        private SurfaceFormat _format;

        private bool _isChecked;

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

        public Color TextColour
        {
            get { return _textColour; }

            set
            {
                if (_textColour != value)
                {
                    _invalidated = true;
                    _textColour = value;
                }
            }
        }

        public int CheckboxHeight
        {
            get { return _checkboxHeight; }

            set
            {
                if (_checkboxHeight != value)
                {
                    _invalidated = true;
                    _checkboxHeight = value;
                }
            }
        }

        public int TextOffsetY
        {
            get { return _textOffsetY; }

            set
            {
                if (_textOffsetY != value)
                {
                    _invalidated = true;
                    _textOffsetY = value;
                }
            }
        }

        public int TextOffsetX
        {
            get { return _textOffsetX; }

            set
            {
                if (_textOffsetX != value)
                {
                    _invalidated = true;
                    _textOffsetX = value;
                }
            }
        }

        public Texture2D CheckboxEmpty
        {
            get { return _checkboxEmpty; }

            set
            {
                if (_checkboxEmpty != value)
                {
                    _invalidated = true;
                    _checkboxEmpty = value;
                }
            }
        }

        public Texture2D CheckboxTicked
        {
            get { return _checkboxTicked; }

            set
            {
                if (_checkboxTicked != value)
                {
                    _invalidated = true;
                    _checkboxTicked = value;
                }
            }
        }

        public int TextHeight
        {
            get { return _textHeight; }

            set
            {
                if (_textHeight != value)
                {
                    _invalidated = true;
                    _textHeight = value;
                }
            }
        }

        public bool IsChecked
        {
            get {return _isChecked;}

            set
            {
                if (_isChecked != value)
                {
                    _invalidated = true;
                    _isChecked = value;
                }
            }
        }


        public CheckboxMaterial(Vector2 size, SurfaceFormat format)
        {
            _textureSize = size;
            _format = format;
            _invalidated = true;
            _invalidatedFont = true;
            _text = "";
            _isChecked = false;
            _textHeight = 0;

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

            // if changed since last time then assemble the checkbox texture and text
            if (_invalidated)
            {
                _invalidated = false;
                Texture2D texture = DrawCheckboxToTexture();
                SetTexture(texture);
            }
        }

        public Vector3 GetTextSize(String text)
        {
            // workout how much the text needs scaling by so that it will fit in the size of the sprite (less the size of the checkbox art)
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

            float textScale = (float)_textHeight / textSize.Y;

            return new Vector3(textSize.X * textScale, textSize.Y * textScale, textScale);
        }

        private Texture2D DrawCheckboxToTexture()
        {
            Vector3 textSize = GetTextSize(_text);

            // do the rendering stuff
            Texture2D checkboxTexture = _isChecked ? _checkboxTicked : _checkboxEmpty;

            // ...create a render target if we haven't already
            if (_myTarget == null)
            {
                if (_format == SurfaceFormat.Unknown)
                {
                    Assert.Fatal(false, "Unknown surface format found when trying to render checkbox material");
                    _format = SurfaceFormat.Color;
                }

                _myTarget = Util.CreateRenderTarget2D(Game.Instance.GraphicsDevice, (int)_textureSize.X, (int)_textureSize.Y, 1, _format);
            }

            // ...setup the device stuff
            TorqueEngineComponent.Instance.Game.GraphicsDevice.SetRenderTarget(0, _myTarget);
            TorqueEngineComponent.Instance.Game.GraphicsDevice.Clear(ClearOptions.Target, new Vector4(0f, 0f, 0f, 0f), 0.0f, 0);

            // ...setup the spritebatch
            SpriteBatch spriteBatch = Game.SpriteBatch;// new SpriteBatch(TorqueEngineComponent.Instance.Game.GraphicsDevice);

            // ...render text to texture
            float checkboxScale = (float)_checkboxHeight / (float)checkboxTexture.Height;
            float totalWidth = (float)checkboxTexture.Width * checkboxScale + textSize.X - _textOffsetX;

            float textScaleX = textSize.Z;

            if (totalWidth > _textureSize.X)
            {
                textScaleX = textSize.Z * (_textureSize.X - checkboxScale * (float)checkboxTexture.Width - _textOffsetX) / textSize.X;
                totalWidth = _textureSize.X;
            }

            spriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.BackToFront, SaveStateMode.None);
            spriteBatch.Draw(
                    checkboxTexture,
                    new Rectangle((int)(_textureSize.X - totalWidth + textSize.X - _textOffsetX), (int)(_textureSize.Y - (float)checkboxTexture.Height * checkboxScale), (int)((float)checkboxTexture.Width * checkboxScale), (int)((float)checkboxTexture.Height * checkboxScale)),
                    new Rectangle(0, 0, checkboxTexture.Width, checkboxTexture.Height),
                    Color.White,
                    0.0f,
                    Vector2.Zero,
                    SpriteEffects.None,
                    0.5f);
            spriteBatch.DrawString(_spriteFont.Instance, _text, new Vector2(_textureSize.X - totalWidth, (_textureSize.Y - textSize.Y) * 0.5f + _textOffsetY), _textColour, 0.0f, Vector2.Zero, new Vector2(textScaleX, textSize.Z), SpriteEffects.None, 0.0f);
            spriteBatch.End();

            // ...reset device stuff
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
