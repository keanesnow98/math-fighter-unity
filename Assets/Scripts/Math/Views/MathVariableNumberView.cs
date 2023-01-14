using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace MathFreak.Math.Views
{
    public class MathVariableNumberView : MathVariableView
    {
        public MathVariableNumberView(MathVariableNumber variable, float h, float spacing, SpriteFont font)
        {
            _texture = DrawStringToTexture(variable.Value.ToString(), h, font);
        }
    }
}
