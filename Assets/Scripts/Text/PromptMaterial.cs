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



namespace MathFreak.Text
{
    public class PromptMaterial : SimpleMaterial, IPreDraw
    {
        public enum EnumAlign { Left, Right, Center };

        private String _text;
        private String _fontName;
        private Color _textColour;
        private int _textHeight;
        private int _textOffsetY;
        private int _spacer;
        private int _iconHeight;
        private Texture2D _icon;
        private bool _textOnRight;
        private EnumAlign _align;

        private bool _invalidatedFont;
        private bool _invalidated;
        private Resource<SpriteFont> _spriteFont;
        private RenderTarget2D _myTarget;
        private Vector2 _textureSize;
        private SurfaceFormat _format;

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

        public int IconHeight
        {
            get { return _iconHeight; }

            set
            {
                if (_iconHeight != value)
                {
                    _invalidated = true;
                    _iconHeight = value;
                }
            }
        }

        public bool TextOnRight
        {
            get { return _textOnRight; }

            set
            {
                if (_textOnRight != value)
                {
                    _invalidated = true;
                    _textOnRight = value;
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

        public int Spacer
        {
            get { return _spacer; }

            set
            {
                if (_spacer != value)
                {
                    _invalidated = true;
                    _spacer = value;
                }
            }
        }

        public Texture2D Icon
        {
            get { return _icon; }

            set
            {
                if (_icon != value)
                {
                    _invalidated = true;
                    _icon = value;
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

        public EnumAlign Align
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


        public PromptMaterial(Vector2 size, SurfaceFormat format)
        {
            _textureSize = size;
            _format = format;
            _invalidated = true;
            _invalidatedFont = true;
            _text = "";
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

            // update the texture?
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

            // ...workout some size and position info
            float iconScale = (float)_iconHeight / (float)_icon.Height;
            float totalWidth = (float)_icon.Width * iconScale + textSize.X + _spacer;

            float textScaleX = textSize.Z;

            if (totalWidth > _textureSize.X)
            {
                float scaleModifier = (_textureSize.X - iconScale * (float)_icon.Width - (float)_spacer) / textSize.X;
                textSize.X *= scaleModifier;
                textScaleX = textSize.Z * scaleModifier;
                totalWidth = _textureSize.X;
            }

            float leftPosX = 0.0f;  // defaults to left aligned

            switch (_align)
            {
                case EnumAlign.Left:
                    leftPosX = 0.0f;
                    break;

                case EnumAlign.Right:
                    leftPosX = (float)_textureSize.X - (float)totalWidth;
                    break;

                case EnumAlign.Center:
                    leftPosX = ((float)_textureSize.X - (float)totalWidth) * 0.5f;
                    break;

                default:
                    Assert.Fatal(false, "Invalid alignment type for prompt: " + _align);
                    leftPosX = 0.0f;
                    break;
            }

            // ...build components into a new texture
            spriteBatch.Begin();

            if (_textOnRight)
            {
                spriteBatch.Draw(_icon, new Rectangle((int)leftPosX, (int)(_textureSize.Y - (float)_icon.Height * iconScale), (int)((float)_icon.Width * iconScale), (int)((float)_icon.Height * iconScale)), Color.White);
                spriteBatch.DrawString(_spriteFont.Instance, _text, new Vector2((int)leftPosX + ((float)_icon.Width * iconScale) + (float)_spacer, (_textureSize.Y - textSize.Y) * 0.5f + _textOffsetY), _textColour, 0.0f, Vector2.Zero, new Vector2(textScaleX, textSize.Z), SpriteEffects.None, 0.0f);
            }
            else
            {
                spriteBatch.DrawString(_spriteFont.Instance, _text, new Vector2(leftPosX, (_textureSize.Y - textSize.Y) * 0.5f + _textOffsetY), _textColour, 0.0f, Vector2.Zero, new Vector2(textScaleX, textSize.Z), SpriteEffects.None, 0.0f); spriteBatch.Draw(_icon, new Rectangle((int)leftPosX, (int)(_textureSize.Y - (float)_icon.Height * iconScale), (int)((float)_icon.Width * iconScale), (int)((float)_icon.Height * iconScale)), Color.White);
                spriteBatch.Draw(_icon, new Rectangle((int)leftPosX + (int)textSize.X + _spacer, (int)(_textureSize.Y - (float)_icon.Height * iconScale), (int)((float)_icon.Width * iconScale), (int)((float)_icon.Height * iconScale)), Color.White);
            }

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
