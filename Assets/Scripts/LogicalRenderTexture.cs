using System;
using System.Collections.Generic;
////using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;



namespace MathFreak
{
    /// <summary>
    /// A logical render texture is a light wrapper for a RenderTarget2D and its associated Texture2D.
    /// What the LRT adds is the ability to remember a 'logical' region in the actual texture so that
    /// we can easily render and use textures that are smaller than the actual texture being used.  This
    /// is needed when reusing rendertargets in constructing MathExpression visual representations, for 
    /// example, because many arbitrary sized RTs are needed which would otherwise require a potentially
    /// very large pool of RTs - using LRTs we can keep the pool to a reasonable size because we only need
    /// a limited number of fixed size RTs instead of an unspecified number of arbitrarliy sized RTs.
    /// </summary>
    public class LogicalRenderTexture
    {
        private RenderTarget2D _renderTarget;
        private Texture2D _texture;
        private Rectangle _region;
        private int _widthIndex;
        private int _heightIndex;

        public RenderTarget2D RenderTarget
        {
            get { return _renderTarget; }
        }

        public Texture2D Texture
        {
            get { return _texture; }
        }

        public Rectangle Region
        {
            get { return _region; }
        }

        /// <summary>
        /// Returns the logical width
        /// </summary>
        public int Width
        {
            get { return _region.Width; }
        }

        /// <summary>
        /// Returns the logical height
        /// </summary>
        public int Height
        {
            get { return _region.Height; }
        }

        public int WidthIndex
        {
            get { return _widthIndex; }
        }

        public int HeightIndex
        {
            get { return _heightIndex; }
        }


        /// <summary>
        /// Specifies width by power instead of actual width as this is internally faster and also less
        /// prone to errors since only power of 2 width and heights are acceptable.  (note: width and height
        /// do not need to be the same value, however).
        /// </summary>
        /// <param name="widthPower2">The power to raise 2 to, in order to calculate the width</param>
        /// <param name="heightPower2">The power to raise 2 to, in order to calculate the height</param>
        public LogicalRenderTexture(int widthPower2, int heightPower2)
        {
            _widthIndex = widthPower2 - LRTPool.MIN_WIDTHPOWER;
            _heightIndex = heightPower2 - LRTPool.MIN_HEIGHTPOWER;

            int width = 1 << widthPower2;
            int height = 1 << heightPower2;

            _renderTarget = new RenderTarget2D(Game.Instance.GraphicsDevice, width, height, 1, SurfaceFormat.Color);
            _region = new Rectangle(0, 0, width, height);
        }

        /// <summary>
        /// For code that needs to work with LRT instances, but when no actual pooled LRT is involved, we
        /// can create an LRT that doesn't have a render target.
        /// </summary>
        /// <param name="texture"></param>
        public LogicalRenderTexture(Texture2D texture)
        {
            _region.Width = texture.Width;
            _region.Height = texture.Height;
            _texture = texture;
        }

        public void SetLogicalSize(int logicalWidth, int logicalHeight)
        {
            _region.Width = logicalWidth;
            _region.Height = logicalHeight;
        }

        /// <summary>
        /// Call this to put the rendered texture in the LRT's Texture property so it can actually be used
        /// </summary>
        public void ResolveTexture()
        {
            _texture = _renderTarget.GetTexture();
        }

        public void Dispose()
        {
            _texture.Dispose();
            _renderTarget.Dispose();
        }
    }
}
