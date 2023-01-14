using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MathFreak.Math.Views;
using GarageGames.Torque.T2D;
using GarageGames.Torque.Core;
using MathFreak.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;


namespace MathFreak.Math
{
    /// <summary>
    /// Represents the division operator in the internal math representation stuff.
    /// </summary>
    public class MathOperatorDivide : MathOperator
    {
        public enum EnumDisplayType { DivSign, Fraction, LongDiv }
        private EnumDisplayType _displayType;


        public MathOperatorDivide()
            : this(EnumDisplayType.Fraction)
        {
        }

        public MathOperatorDivide(EnumDisplayType displayType)
        {
            _displayType = displayType;
        }

        public override EnumPemdasLevel PemdasLevel()
        {
            return EnumPemdasLevel.Divide;
        }

        public override string TextRepresentation()
        {
            return LeftParam.TextRepresentation() + ApplyInsertionString("/ ") + RightParam.TextRepresentation();
        }

        public override MathVariableNumber Evaluate()
        {
            MathVariableNumber leftVar = EvaluateLeftParam();

            if (leftVar == null) return null;

            MathVariableNumber rightVar = EvaluateRightParam();

            if (rightVar == null) return null;

            return new MathVariableNumber((leftVar as MathVariableNumber).Value / (rightVar as MathVariableNumber).Value);
        }

        public override void AdjustDivisionParamsForIntegerResult()
        {
            float value1 = EvaluateLeftParam().Value;
            float value2 = EvaluateRightParam().Value;

            // if righthand param evaluates to zero then we need to change this so that we
            // don'thave divide by zero
            if (value2 == 0.0f)
            {
                // righthand param should be a parenthesized subexpression, so we will just add a one to the subexpression so that is is no longer zero
                Assert.Fatal(RightParam is MathOperatorParenthesis, "righthand param is expected to be parentheses");

                MathOperatorParenthesis parenthesesOp = (RightParam as MathOperatorParenthesis);
                MathOperatorAdd newOp = new MathOperatorAdd();
                newOp.LeftParam = new MathVariableNumber(1);
                newOp.RightParam = parenthesesOp.LeftParam;
                parenthesesOp.LeftParam = newOp;

                // update value2 as the righthand param has changed now
                value2 = EvaluateRightParam().Value;

                Assert.Fatal(value2 == 1, "righthand param should evaluate to 1");
            }

            // if result is non integer then we need will adjust the left param by multiplying by
            // the righthand param to make the overall result an integer
            float result = value1 / value2;

            if (result.ToString() != ((int)result).ToString())  // yeah, this is a funky way to check if non integer - can't compare to the result of System.Math.Truncate() because that actually isn't capable of reliably truncating a float (who'd have guessed!)
            {
                MathOperatorMultiply newOp = new MathOperatorMultiply();
                newOp.LeftParam = new MathVariableNumber(value2);
                newOp.RightParam = LeftParam;
                LeftParam = newOp;
            }
        }

        public override MathElement Simplify_SubtractExponents()
        {
            MathElement currentNode = this;

            // simplify our params first
            MathElement leftElement = LeftParam;
            MathElement rightElement = RightParam;

            if (leftElement is MathOperator)
            {
                leftElement = (leftElement as MathOperator).Simplify_SubtractExponents();
            }

            if (rightElement is MathOperator)
            {
                rightElement = (rightElement as MathOperator).Simplify_SubtractExponents();
            }

            // for params that are exponent operators we will store the exponent and grab the subtree being raised to the power
            // else we will stick with the full subtree and an exponent value of 1
            float leftExponent = 1.0f;
            float rightExponent = 1.0f;

            if (leftElement is MathOperatorExponent)
            {
                MathOperator op = leftElement as MathOperator;
                leftExponent = (op.RightParam as MathVariableNumber).Value; // note: assumes that it's a number (if letters can be used later then we would need to create a subtree instead)
                leftElement = op.LeftParam;
            }

            if (rightElement is MathOperatorExponent)
            {
                MathOperator op = rightElement as MathOperator;
                rightExponent = (op.RightParam as MathVariableNumber).Value; // note: assumes that it's a number (if letters can be used later then we would need to create a subtree instead)
                rightElement = op.LeftParam;
            }

            // if the subtrees are identical then we can subtract the exponents, one from the other
            if (leftElement.EqualsSubtree(rightElement))
            {
                // parenthesize the common term if necessary
                if (!(leftElement is MathVariable || leftElement is MathOperatorParenthesis))
                {
                    MathOperatorParenthesis parenthesisNode = new MathOperatorParenthesis();
                    parenthesisNode.LeftParam = leftElement;
                    leftElement = parenthesisNode;
                }

                // based on the exponent that we get after subtraction there are 4 cases to handle
                float newExponent = leftExponent - rightExponent;

                // case 1 - exponent reduces to > 1 and so we can replace ourselves with the common term and exponent
                if (newExponent > 1.0f)
                {
                    MathOperatorExponent exponentNode = new MathOperatorExponent();
                    exponentNode.LeftParam = leftElement;
                    exponentNode.RightParam = new MathVariableNumber(newExponent);
                    currentNode = exponentNode;
                    ParentNode.ReplaceParam(this, currentNode);
                }
                // case 2 - exponent reduces to 1 and so we can replace ourselves with just the common term
                else if (newExponent == 1.0f)
                {
                    currentNode = leftElement;
                    ParentNode.ReplaceParam(this, currentNode);
                }
                // case 3 - exponent reduces to 0 and so we can replace ourselves a number of value 1.0f
                else if (newExponent == 0.0f)
                {
                    MathVariableNumber numberNode = new MathVariableNumber(1.0f);
                    currentNode = numberNode;
                    ParentNode.ReplaceParam(this, currentNode);
                }
                // case 4 - exponent goes negative and so we must replace the numerator with a number of value 1.0f and and modify the denominator's exponent (to be original denominator exponent minus the original numerator exponent)
                else if (newExponent < 0.0f)
                {
                    MathVariableNumber numberNode = new MathVariableNumber(1.0f);
                    LeftParam = numberNode;

                    // if new exponent for numerator is 1 then replace with just the common term
                    if (rightExponent - leftExponent == 1)
                    {
                        RightParam = leftElement;
                    }
                    // else modify the exponent
                    else
                    {
                        (RightParam as MathOperatorExponent).RightParam = new MathVariableNumber(rightExponent - leftExponent);
                    }
                }
                else
                {
                    Assert.Fatal(false, "Unhandled case in Simplify_SubtractExponents()");
                }
            }

            // return to the caller whatever is now at this point in the tree (us if we weren't replaced by a number or the number if we were replaced)
            return currentNode;
        }

        public override MathFreak.Math.Views.MathElementView GenerateView(float h, float spacing, SpriteFont font)
        {
            switch (_displayType)
            {
                case EnumDisplayType.DivSign:
                    return new MathOperatorDivideDivSignView(this, h, spacing, font);
                    //break;

                case EnumDisplayType.Fraction:
                    return new MathOperatorDivideFractionView(this, h, spacing, font);
                    //break;

                case EnumDisplayType.LongDiv:
                    return new MathOperatorDivideLongDivView(this, h, spacing, font);
                    //break;

                default:
                    Assert.Fatal(false, "Unrecognized display type for division operator: " + _displayType);
                    return new MathOperatorDivideFractionView(this, h, spacing, font);  // use fraction type as default
                    //break;
            }
        }
    }
}
