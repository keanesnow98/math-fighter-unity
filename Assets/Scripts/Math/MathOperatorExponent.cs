using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MathFreak.Math.Views;
using GarageGames.Torque.Core;

namespace MathFreak.Math
{
    /// <summary>
    /// Represents the exponent operator (raises a number to a power) in the internal math representation stuff.
    /// </summary>
    public class MathOperatorExponent : MathOperator
    {
        public enum EnumType { Power, Root }
        private EnumType _type;


        public MathOperatorExponent()
            : this(EnumType.Power)
        {
        }

        public MathOperatorExponent(EnumType type)
        {
            _type = type;
        }

        public override EnumPemdasLevel PemdasLevel()
        {
            return EnumPemdasLevel.Exponent;
        }

        public override string TextRepresentation()
        {
            string op = (_type == EnumType.Power ? "^ " : "ROOT ");
            return LeftParam.TextRepresentation() + ApplyInsertionString(op) + RightParam.TextRepresentation();
        }

        public override MathVariableNumber Evaluate()
        {
            MathVariableNumber leftVar = EvaluateLeftParam();

            if (leftVar == null) return null;

            MathVariableNumber rightVar = EvaluateRightParam();

            if (rightVar == null) return null;

            // exponent has higher pemdas than negation - so we'll handle that here

            // work out the absolute value of left param raised to a power of right param
            float power = (_type == EnumType.Power ? rightVar.Value : -rightVar.Value);
            float raisedToPower = (float)System.Math.Pow(leftVar.Value, power);

            return new MathVariableNumber(raisedToPower);
        }

        public override MathFreak.Math.Views.MathElementView GenerateView(float charHeight, float spacing, Microsoft.Xna.Framework.Graphics.SpriteFont font)
        {
            switch (_type)
            {
                case EnumType.Power:
                    return new MathOperatorExponentView(this, charHeight, spacing, font);
                //break;

                case EnumType.Root:
                    return new MathOperatorExponentRootView(this, charHeight, spacing, font);
                //break;

                default:
                    Assert.Fatal(false, "Unrecognized display type for exponent/root operator: " + _type);
                    return new MathOperatorExponentView(this, charHeight, spacing, font);  // use power as default
                //break;
            }
        }
    }
}
