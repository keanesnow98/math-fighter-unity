using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;

namespace MathFreak.Math.Views
{
    public class MathOperatorExpressionRootView : MathOperatorView
    {
        public MathOperatorExpressionRootView(MathOperatorExpressionRoot op, float h, float spacing, SpriteFont font)
        {
            _texture = op.LeftParam.GenerateView(h, spacing, font).Texture;
        }
    }
}
