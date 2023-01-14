using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MathFreak.Math.Views;
using Microsoft.Xna.Framework.Graphics;

namespace MathFreak.Math
{
    /// <summary>
    /// This variable contains a letter (single character not a string) and also whether or not
    /// the variable is negated (stored as a boolean).
    /// </summary>
    public class MathVariableLetter : MathVariable
    {
        private char _letter;
        private bool _negated;

        public char Letter
        {
            get { return _letter; }
        }

        public bool Negated
        {
            get { return _negated; }
        }

        public string StringToken
        {
            get { return ((_negated ? "-" : "") + _letter); }
        }

        public MathVariableLetter(bool negated, char letter)
        {
            _negated = negated;
            _letter = letter;
        }

        public override string ToString()
        {
            return "MathVariableLetter:" + StringToken;
        }

        public override EnumPemdasLevel PemdasLevel()
        {
            return EnumPemdasLevel.Variable;
        }

        public override string TextRepresentation()
        {
            return ApplyInsertionString(StringToken + " ");
        }

        public override MathFreak.Math.Views.MathElementView GenerateView(float h, float spacing, SpriteFont font)
        {
            return new MathVariableLetterView(this, h, spacing, font);
        }
    }
}
