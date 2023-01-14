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
using System.Xml.Serialization;
using System.Diagnostics;
using MathFreak.Math.Views;
using MathFreak.Math;



namespace MathFreak.Text
{
    /// <summary>
    /// This component allows you set T2DStaticSprites to having text rendered onto them instead of
    /// a normal bitmap material.  Similar to TextComponent except that this once can handle multiline
    /// text, word wrap, and arbitrary textures inserted in the lines of text.
    /// </summary>
    [TorqueXmlSchemaType]
    public class MathMultiTextComponent : TorqueComponent, IPreDraw
    {
        //======================================================
        #region Static methods, fields, constructors
        #endregion

        //======================================================
        #region Constructors
        #endregion

        //======================================================
        #region Public properties, operators, constants, and enums

        public enum EnumFitMode { None, SquashTextIfTooBig };//, StretchTextIfTooSmall, FitTextToWidth, TruncateText };

        public String TextValue
        {
            get { return _text; }

            set
            {
                if (_text != value)
                {
                    _invalidated = true;
                }

                _text = value;

                if (_text == null || _text.Length == 0)
                {
                    _text = " ";
                }
            }
        }

        public Game.EnumMathFreakFont Font
        {
            get { return _fontEnum; }

            set
            {
                if (_fontEnum != value)
                {
                    _invalidated = true;
                }

                _fontEnum = value;
            }
        }

        public TextMaterial.Alignment Align
        {
            get { return _align; }

            set
            {
                if (_align != value)
                {
                    _invalidated = true;
                }

                _align = value;
            }
        }

        // A Vector4 instead of Color because the torque scene editor can't present a Color object for editing (Vector4 is as an acceptable second choice)
        public Vector4 RGBA
        {
            get { return _rgba; }
            
            set
            {
                if (_rgba != value)
                {
                    _invalidated = true;
                }

                _rgba = value;

                _color = new Color(_rgba);
            }
        }

        [TorqueXmlSchemaType(DefaultValue = "0 0 0 0", IsDefaultValueOf = true)]
        public Vector4 BackgroundRGBA
        {
            get { return _background_rgba; }

            set
            {
                if (_background_rgba != value)
                {
                    _invalidated = true;
                }

                _background_rgba = value;

                _bgcolor = new Color(_background_rgba);
            }
        }

        public float CharacterHeight
        {
            get { return _charHeight; }

            set
            {
                if (_charHeight != value)
                {
                    _invalidated = true;
                }

                _charHeight = value;
            }
        }

        public float LineSpacing
        {
            get { return _lineSpacing; }

            set
            {
                if (_lineSpacing != value)
                {
                    _invalidated = true;
                }

                _lineSpacing = value;
            }
        }

        [TorqueXmlSchemaType(DefaultValue = "None")]
        public EnumFitMode FitMode
        {
            get { return _fitMode; }
            set { _fitMode = value; }
        }

        [XmlIgnore]
        public float MaxLineWidth
        {
            get { return _maxLineWidth; }

            set
            {
                if (_maxLineWidth != value)
                {
                    _invalidated = true;
                }

                _maxLineWidth = value;
            }
        }

        #endregion

        //======================================================
        #region Public methods

        public override void CopyTo(TorqueComponent obj)
        {
            base.CopyTo(obj);

            MathMultiTextComponent obj2 = obj as MathMultiTextComponent;

            obj2.TextValue = TextValue;
            obj2.RGBA = RGBA;
            obj2.Align = Align;
            obj2.Font = Font;
            obj2.LineSpacing = LineSpacing;
            obj2.CharacterHeight = CharacterHeight;
        }

        public void PreDraw(float dt)
        {
            // if not invalidated then nothing to do here - just return
            if (!_invalidated) return;

            _invalidated = false;

            // else we need to build our texture and up date our owner sprite with it

            // get the font
            _font = Game.GetEnumeratedFont(_fontEnum);

            // get the surface format if we haven't already
            if (_surfaceformat == SurfaceFormat.Unknown)
            {
                _surfaceformat = ((_sprite.Material as SimpleMaterial).Texture.Instance as Texture2D).Format;
            }

            // create a new material if we haven't already
            if (_material == null)
            {
                _material = new SimpleMaterial();

                // and set it as the sprite's material (this is the material instance that we will set the texture on later)
                _sprite.Material = _material;
            }

            // setup the spritebatch
            if (_spriteBatch == null)
            {
                _spriteBatch = Game.SpriteBatch;//new SpriteBatch(TorqueEngineComponent.Instance.Game.GraphicsDevice);
            }

            // reset global modifiers
            _heightScale = 1.0f;

            // the string can contain references to stuff that aren't normal text and need special rendering (such as a math expression)
            // so first split the string up into a list of strings containing either normal or special text.
            string[] segments = _text.Split('#');

            // now split the normal strings up into words, so we have a list of 'words', some of which are normal words
            // and some of which are special strings.
            List<string> words = new List<string>();

            foreach (string seg in segments)
            {
                // if it's a special string then just treat it as a 'word'
                if (seg[0] == '@')
                {
                    words.Add(seg);
                }
                // else it's normal text so split it into words and add the individual words to the list
                else
                {
                    string[] textWords = seg.Split(' ');

                    foreach (string tw in textWords)
                    {
                        words.Add(tw.Replace("&nbsp", " "));    // handles non-breaking spaces
                    }
                }
            }

            // workout what can fit on a line and apply word wrapping normal text as required
            // as we go we will be rendering the normal text and special text to textures to produce a list of lines of textures
            List<List<LogicalRenderTexture>> lines = new List<List<LogicalRenderTexture>>();
            List<LogicalRenderTexture> currentLine = new List<LogicalRenderTexture>();
            LogicalRenderTexture texture;
            float currentWidth = 0.0f;

            foreach (string w in words)
            {
                // ignore empty strings (multiple consecutive spaces can cause this)
                if (w.Length == 0) continue;

                // render the 'word' to a texture
                if (w[0] == '@')
                {
                    texture = RenderSpecialText(w);
                }
                else
                {
                    texture = RenderNormalText(w);
                }

                if (texture == null) continue;  // some text is there to apply global modifiers, for example, rather than actually rendering stuff

                // if this word doesn't take us beyond the end of the line (or the current line is empty) then just add the texture to the line
                if (currentWidth + texture.Width <= _maxLineWidth || currentLine.Count == 0) // note: the first line can be empty of course so we wouldn't want to create *another* line in that situation
                {
                    currentLine.Add(texture);
                    currentWidth += texture.Width;
                }
                // else we need to put the texture on a new line
                else
                {
                    // strip the trailing space from the current line
                    LRTPool.Instance.ReleaseLRT(currentLine[currentLine.Count - 1]);
                    currentLine.RemoveAt(currentLine.Count - 1);

                    // create the new line and add the texture
                    lines.Add(currentLine);
                    currentLine = new List<LogicalRenderTexture>();
                    currentLine.Add(texture);
                    currentWidth = texture.Width;
                }

                // add a space (can be stripped off again later if need be, but at this point we don't know so we just assume a space should be added)
                LogicalRenderTexture spaceTexture = RenderNormalText(" ");
                currentLine.Add(spaceTexture);
                currentWidth += spaceTexture.Width;
            }

            // make sure to add the final line to the list
            if (currentLine.Count > 0)
            {
                lines.Add(currentLine);

                // strip the trailing space from the current line (and don't forget to dispose of the associated lrt!!!!!)
                LRTPool.Instance.ReleaseLRT(currentLine[currentLine.Count - 1]);
                currentLine.RemoveAt(currentLine.Count - 1);
            }

            // add up the heights of all the lines
            // and also find the widest line
            // We'll also store the line heights and widths while we're here as we'll use them again later
            float textureHeight = 0.0f;
            float textureWidth = 0.0f;
            Vector2[] lineSizes = new Vector2[lines.Count];
            int i = 0;

            foreach (List<LogicalRenderTexture> line in lines)
            {
                float lineWidth = 0.0f;
                float lineHeight = 0.0f;

                foreach (LogicalRenderTexture tex in line)
                {
                    lineWidth += tex.Width;

                    if (tex.Height > lineHeight)
                    {
                        lineHeight = tex.Height;
                    }
                }

                textureHeight += lineHeight;

                if (lineWidth > textureWidth)
                {
                    textureWidth = lineWidth;
                }

                lineSizes[i] = new Vector2(lineWidth, lineHeight);
                i++;
            }

            // adjust for linespacing
            textureHeight += (lines.Count - 1) * (_lineSpacing * _heightScale);

            // if not centered then force textureWidth to original width so that we can do text alignment stuff
            // But only force it if not shrinking to fit or the texture would be smaller than the original size
            if (_align != TextMaterial.Alignment.centered && (FitMode != EnumFitMode.SquashTextIfTooBig || textureWidth < _originalSize.X))
            {
                textureWidth = _originalSize.X;
            }

            // silliness check - can't have a zero sized texture
            if ((int)textureWidth == 0) textureWidth = 1.0f;
            if ((int)textureHeight == 0) textureHeight = 1.0f;

            // create a render target of the right size (dispose the existing one if there is one)
            if (_renderTarget != null)
            {
                _material.Texture.Instance.Dispose();
                _renderTarget.Dispose();
            }

            // create rendertarget - do silliness check to make sure not bigger than backbuffer et al.
            if (textureWidth > 1280)
            {
                _renderTarget = Util.CreateRenderTarget2D(Game.Instance.GraphicsDevice, 1280, (int)textureHeight, 1, _surfaceformat);
            }
            else
            {
                _renderTarget = Util.CreateRenderTarget2D(Game.Instance.GraphicsDevice, (int)textureWidth, (int)textureHeight, 1, _surfaceformat);
            }

            // setup the device stuff
            TorqueEngineComponent.Instance.Game.GraphicsDevice.SetRenderTarget(0, _renderTarget);
            TorqueEngineComponent.Instance.Game.GraphicsDevice.Clear(ClearOptions.Target, _bgcolor, 0.0f, 0);

            // start the spritebatch
            _spriteBatch.Begin();

            // render the text-line textures to the 'master' texture
            float linePosY = 0.0f;
            float posX;
            float posY;
            i = 0;

            foreach (List<LogicalRenderTexture> line in lines)
            {
                //      workout where to start the line horizontally
                Vector2 lineSize = lineSizes[i];

                switch (_align)
                {
                    case TextMaterial.Alignment.left:
                        posX = 0.0f;
                        break;

                    case TextMaterial.Alignment.right:
                        posX = textureWidth - lineSize.X;
                        break;

                    case TextMaterial.Alignment.centered:
                        posX = (textureWidth - lineSize.X) * 0.5f;
                        break;

                    default:    // this isn't going to happen, but keeps the compiler happy (else gives a use of undefined var posX error further down)
                        posX = 0.0f;
                        break;
                }

                //      render all the textures for this line
                foreach (LogicalRenderTexture tex in line)
                {
                    // note: textures are centered vertically based on the height of the line
                    posY = linePosY + (lineSize.Y - tex.Height) * 0.5f;
                    _spriteBatch.Draw(tex.Texture, new Vector2(posX, posY), tex.Region, _color);

                    // increment to the next horizontal position
                    posX += tex.Width;

                    // done with the texture now?
                    //if (tex != _spaceTexture)
                    //{
                        LRTPool.Instance.ReleaseLRT(tex);
                    //}
                }

                // get ready for the next line
                linePosY += lineSize.Y + (_lineSpacing * _heightScale);
                i++;
            }

            // end the spritebatch
            _spriteBatch.End();

            // reset device stuff
            TorqueEngineComponent.Instance.Game.GraphicsDevice.SetRenderTarget(0, null);

            // assign the texture to the sprite's material
            Texture2D finalTexture = _renderTarget.GetTexture();
            _material.SetTexture(finalTexture);

            switch (_fitMode)
            {
                case EnumFitMode.None:
                    _sprite.Size = new Vector2(finalTexture.Width, finalTexture.Height);
                    break;

                case EnumFitMode.SquashTextIfTooBig:
                    if (finalTexture.Width > (int)_originalSize.X)
                    {
                        _sprite.Size = new Vector2(_originalSize.X, finalTexture.Height);
                    }
                    else
                    {
                        _sprite.Size = new Vector2(finalTexture.Width, finalTexture.Height);
                    }
                    break;

                default:
                    Assert.Warn(false, "Fit mode not handled yet: " + _fitMode);
                    break;
            }

            // cleanup
            //LRTPool.Instance.ReleaseLRT(spaceTexture);            
        }

        #endregion

        //======================================================
        #region Private, protected, internal methods

        private Vector3 GetTextSize(String text)
        {
            return GetTextSize(text, 1.0f);
        }

        private Vector3 GetTextSize(String text, float scaleFactor)
        {
            Assert.Fatal(text != null && text.Length != 0, "Trying to calculate text size for an empty or null string");

            Vector2 textSize = _font.MeasureString(text);

            float textScale = (_charHeight * _heightScale * scaleFactor) / textSize.Y;

            return new Vector3(textSize.X * textScale, textSize.Y * textScale, textScale);
        }

        private LogicalRenderTexture RenderSpecialText(string text)
        {
            if (text.StartsWith("@math{"))
            {
                return RenderMathExpression(text.Substring(6, text.Length - 7));
            }
            else if (text.StartsWith("@newline{"))
            {
                return RenderNewLine(text.Substring(9, text.Length - 10));
            }
            else if (text.StartsWith("@height{"))
            {
                return ModifyHeight(text.Substring(8, text.Length - 9));
            }
            else if (text.StartsWith("@math_limit{"))
            {
                return RenderMathLimit(text.Substring(12, text.Length - 13));
            }
            else
            {
                Assert.Fatal(false, "unrecognized special text type: " + text);
                return RenderNormalText(text);
            }
        }

        private LogicalRenderTexture RenderNormalText(string text)
        {
            return RenderNormalText(text, 1.0f);
        }

        private LogicalRenderTexture RenderNormalText(string text, float scaleFactor)
        {
            Assert.Fatal(_surfaceformat != SurfaceFormat.Unknown, "Unknown surface format");

            // work out how much space the text will take up when rendered
            Vector3 textSize = GetTextSize(text, scaleFactor);

            // create a render target
            LogicalRenderTexture lrt = LRTPool.Instance.AcquireLRT((int)textSize.X, (int)textSize.Y);

            // setup the device stuff
            TorqueEngineComponent.Instance.Game.GraphicsDevice.SetRenderTarget(0, lrt.RenderTarget);
            TorqueEngineComponent.Instance.Game.GraphicsDevice.Clear(ClearOptions.Target, Color.TransparentBlack, 0.0f, 0);

            // render text to texture
            _spriteBatch.Begin();
            _spriteBatch.DrawString(_font, text, Vector2.Zero, _color, 0.0f, Vector2.Zero, textSize.Z, SpriteEffects.None, 0.0f);
            _spriteBatch.End();

            // reset device stuff
            TorqueEngineComponent.Instance.Game.GraphicsDevice.SetRenderTarget(0, null);

            // resolve and return the newly rendered texture
            lrt.ResolveTexture();
            return lrt;
        }

        private LogicalRenderTexture RenderMathExpression(string text)
        {
            string expressionString; 
            float charHeightModifier;

            // check for a char height modifier being specified and get the modifier and expression string as appropriate
            if (text[0] == '{')
            {
                int expressionStartPos = text.IndexOf('}') + 1;

                charHeightModifier = float.Parse(text.Substring(1, expressionStartPos - 2));
                //Debug.WriteLine("char height modifier found: " + charHeightModifier);

                expressionString = text.Substring(expressionStartPos);
            }
            else
            {
                charHeightModifier = 1.0f;
                expressionString = text;
            }

            //Debug.WriteLine("Rendering math expression as special text: " + expressionString);

            MathExpression expression = MathExpression.Parse(expressionString);
            return expression.GenerateTexture(_charHeight * _heightScale * charHeightModifier, _charHeight * _heightScale * charHeightModifier * 0.1f, _font, Vector4.Zero);
        }

        private LogicalRenderTexture RenderNewLine(string text)
        {
            // setup the texture size params
            int w = (int)_maxLineWidth;
            int h = 1;

            // check if there is a char height modifier specified and adjust the texture height as necessary
            if (text.Length > 0)
            {
                //Debug.WriteLine("newline char height modifier: " + text);
                h = (int)((float)h * float.Parse(text));
            }

            //Debug.WriteLine("newline height: " + h);

            // create a render target
            LogicalRenderTexture lrt = LRTPool.Instance.AcquireLRT(w, h);

            // setup the device stuff
            TorqueEngineComponent.Instance.Game.GraphicsDevice.SetRenderTarget(0, lrt.RenderTarget);
            TorqueEngineComponent.Instance.Game.GraphicsDevice.Clear(ClearOptions.Target, Color.TransparentBlack, 0.0f, 0);

            // render text to texture (draw an empty string)
            _spriteBatch.Begin();
            _spriteBatch.DrawString(_font, " ", Vector2.Zero, _color, 0.0f, Vector2.Zero, 1.0f, SpriteEffects.None, 0.0f);
            _spriteBatch.End();

            // reset device stuff
            TorqueEngineComponent.Instance.Game.GraphicsDevice.SetRenderTarget(0, null);

            // resolve and return the newly rendered texture
            lrt.ResolveTexture();
            return lrt;
        }

        private LogicalRenderTexture RenderMathLimit(string text)
        {
            Assert.Fatal(_surfaceformat != SurfaceFormat.Unknown, "Unknown surface format");

            // get the parameters to use in the math limit symbol display
            string[] limitParams = text.Split(',');

            // render the component parts
            LogicalRenderTexture texLimit = RenderNormalText("lim");
            LogicalRenderTexture texParamX = RenderNormalText(limitParams[0], 0.7f);
            LogicalRenderTexture texArrow = RenderNormalText("#", 0.7f);
            LogicalRenderTexture texParamY = RenderNormalText(limitParams[1], 0.7f);

            // get the lineheight adjustment param - most fonts will result in too big a gap under the "lim" text so need to adjust for that
            float lineHeightAdjustment = float.Parse(limitParams[2]);
            int adjustedLimHeight = texLimit.Height + (int)((float)lineHeightAdjustment * texLimit.Height);

            // work out how big the final texture needs to be
            int widthOfParams = texParamX.Width + texArrow.Width + texParamY.Width;
            int heightOfParams = texParamX.Height;

            if (texArrow.Height > heightOfParams)
            {
                heightOfParams = texArrow.Height;
            }

            if (texParamY.Height > heightOfParams)
            {
                heightOfParams = texParamY.Height;
            }

            int totalWidth = widthOfParams;
            int totalHeight = adjustedLimHeight + heightOfParams;

            if (texLimit.Width > totalWidth)
            {
                totalWidth = texLimit.Width;
            }

            // create a render target
            LogicalRenderTexture lrt = LRTPool.Instance.AcquireLRT(totalWidth, totalHeight);

            // setup the device stuff
            TorqueEngineComponent.Instance.Game.GraphicsDevice.SetRenderTarget(0, lrt.RenderTarget);
            TorqueEngineComponent.Instance.Game.GraphicsDevice.Clear(ClearOptions.Target, Color.TransparentBlack, 0.0f, 0);

            // composite the component textures into the final texture
            _spriteBatch.Begin();
            _spriteBatch.Draw(texLimit.Texture, new Vector2(((float)totalWidth - (float)texLimit.Width) * 0.5f, 0.0f), texLimit.Region, Color.White, 0.0f, Vector2.Zero, 1.0f, SpriteEffects.None, 0.0f);
            float posX = ((float)totalWidth - (float)widthOfParams) * 0.5f;
            _spriteBatch.Draw(texParamX.Texture, new Vector2(posX, (float)adjustedLimHeight + (((float)heightOfParams - (float)texParamX.Height) * 0.5f)), texParamX.Region, Color.White, 0.0f, Vector2.Zero, 1.0f, SpriteEffects.None, 0.0f);
            posX += texParamX.Width;
            _spriteBatch.Draw(texArrow.Texture, new Vector2(posX, (float)adjustedLimHeight + (((float)heightOfParams - (float)texArrow.Height) * 0.5f)), texArrow.Region, Color.White, 0.0f, Vector2.Zero, 1.0f, SpriteEffects.None, 0.0f);
            posX += texArrow.Width;
            _spriteBatch.Draw(texParamY.Texture, new Vector2(posX, (float)adjustedLimHeight + (((float)heightOfParams - (float)texParamY.Height) * 0.5f)), texParamY.Region, Color.White, 0.0f, Vector2.Zero, 1.0f, SpriteEffects.None, 0.0f);
            _spriteBatch.End();

            // reset device stuff
            TorqueEngineComponent.Instance.Game.GraphicsDevice.SetRenderTarget(0, null);

            // release textures we've finished with
            LRTPool.Instance.ReleaseLRT(texLimit);
            LRTPool.Instance.ReleaseLRT(texParamX);
            LRTPool.Instance.ReleaseLRT(texArrow);
            LRTPool.Instance.ReleaseLRT(texParamY);

            // resolve and return the newly rendered texture
            lrt.ResolveTexture();
            return lrt;
        }

        private LogicalRenderTexture ModifyHeight(string text)
        {
            // get the height modifier and update global properties accordingly
            if (text.Length > 0)
            {
                //Debug.WriteLine("newline char height modifier: " + text);
                _heightScale = float.Parse(text);
            }

            return null;
        }

        protected override bool _OnRegister(TorqueObject owner)
        {
            if (!base._OnRegister(owner) || !(owner is T2DStaticSprite))
                return false;

            _sprite = (owner as T2DStaticSprite);
            _invalidated = true;    // make sure to do the first-time texture building
            _surfaceformat = SurfaceFormat.Unknown;

            // auto-detect the width of the owner sprite as our max line width
            _maxLineWidth = _sprite.Size.X;

            _originalSize = _sprite.Size;

            PreDrawManager.Instance.Register(this);

            return true;
        }

        protected override void _OnUnregister()
        {
            PreDrawManager.Instance.UnRegister(this);

            base._OnUnregister();
        }

        #endregion

        //======================================================
        #region Private, protected, internal fields

        private T2DStaticSprite _sprite;

        private SpriteBatch _spriteBatch;

        private String _text;
        private Game.EnumMathFreakFont _fontEnum;
        private SpriteFont _font;
        private TextMaterial.Alignment _align;
        private Vector4 _rgba;
        private Color _color;
        private Vector4 _background_rgba;
        private Color _bgcolor;
        private bool _invalidated;
        private float _maxLineWidth;
        private float _charHeight;
        private float _lineSpacing;
        private SurfaceFormat _surfaceformat;
        private SimpleMaterial _material;
        private RenderTarget2D _renderTarget;
        private EnumFitMode _fitMode = EnumFitMode.None;
        private Vector2 _originalSize;
        private float _heightScale;

        #endregion
    }
}
