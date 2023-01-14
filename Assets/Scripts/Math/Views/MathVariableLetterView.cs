using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace MathFreak.Math.Views
{
    public class MathVariableLetterView : MathVariableView
    {
        public MathVariableLetterView(MathVariableLetter variable, float h, float spacing, SpriteFont font)
        {
            _texture = DrawStringToTexture(variable.StringToken, h, font);
        }
    }
}
