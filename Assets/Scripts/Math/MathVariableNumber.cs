using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MathFreak.Math.Views;
using Microsoft.Xna.Framework.Graphics;



namespace MathFreak.Math
{
    /// <summary>
    /// This is the simplest type of variable.  It just contains a single value.
    /// Note that this value is stored as a decimal although in Math Freak we will only output
    /// integers to the screen.
    /// </summary>
    public class MathVariableNumber : MathVariable
    {
        private float _value;

        private static MathVariableNumber _CONST_ZERO;
        private static MathVariableNumber _CONST_ONE;

        public float Value
        {
            get { return _value; }
        }

        public static MathVariableNumber ZERO
        {
            get
            {
                if (_CONST_ZERO == null)
                {
                    _CONST_ZERO = new MathVariableNumber(0);
                }

                return _CONST_ZERO;
            }
        }

        public static MathVariableNumber ONE
        {
            get
            {
                if (_CONST_ONE == null)
                {
                    _CONST_ONE = new MathVariableNumber(1);
                }

                return _CONST_ONE;
            }
        }

        public MathVariableNumber(float value)
        {
            _value = value;
        }

        public override string ToString()
        {
            return "MathVariableNumber:" + _value;
        }

        public override EnumPemdasLevel PemdasLevel()
        {
            return EnumPemdasLevel.Variable;
        }

        public override string TextRepresentation()
        {
            return ApplyInsertionString(_value.ToString() + " ");
        }

        public override MathFreak.Math.Views.MathElementView GenerateView(float h, float spacing, SpriteFont font)
        {
            return new MathVariableNumberView(this, h, spacing, font);
        }
    }
}
