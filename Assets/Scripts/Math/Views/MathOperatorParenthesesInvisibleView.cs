using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using GarageGames.Torque.Core;
using GarageGames.Torque.XNA;
using Microsoft.Xna.Framework;



namespace MathFreak.Math.Views
{
    public class MathOperatorParenthesesInvisibleView : MathOperatorView
    {
        public MathOperatorParenthesesInvisibleView(MathOperatorParenthesis op, float h, float spacing, SpriteFont font)
        {
            // get the texture for our parameter - we aren't displaying parentheses in this view
            _texture = op.LeftParam.GenerateView(h, spacing, font).Texture;
        }
    }
}
