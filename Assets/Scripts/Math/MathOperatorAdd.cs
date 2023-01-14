using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MathFreak.Math.Views;
using GarageGames.Torque.Core;

namespace MathFreak.Math
{
    /// <summary>
    /// Represents the addition operator in the internal math representation stuff.
    /// </summary>
    public class MathOperatorAdd : MathOperator
    {
        public override EnumPemdasLevel PemdasLevel()
        {
            return EnumPemdasLevel.AddSubract;
        }

        public override string TextRepresentation()
        {
            return LeftParam.TextRepresentation() + ApplyInsertionString("+ ") + RightParam.TextRepresentation();
        }

        public override MathVariableNumber Evaluate()
        {
            MathVariableNumber leftVar = EvaluateLeftParam();

            if (leftVar == null) return null;

            MathVariableNumber rightVar = EvaluateRightParam();

            if (rightVar == null) return null;

            return new MathVariableNumber((leftVar as MathVariableNumber).Value + (rightVar as MathVariableNumber).Value);
        }

        public override MathElement Simplify_CombineIdenticalTermsInAddition()
        {
            MathElement currentNode = this;

            // simplify our params first
            MathElement leftElement = LeftParam;
            MathElement rightElement = RightParam;

            if (leftElement is MathOperator)
            {
                leftElement = (leftElement as MathOperator).Simplify_CombineIdenticalTermsInAddition();
            }

            if (rightElement is MathOperator)
            {
                rightElement = (rightElement as MathOperator).Simplify_CombineIdenticalTermsInAddition();
            }

            // by default we will treat the whole subtree of each of our params as a term being multipied by 1.0f
            float leftMultiplier = 1.0f;
            float rightMultiplier = 1.0f;

            // if a param is a multiplier operator then we need to check if we should grab it's params as our multiplier-and-term (i.e it's left param is a number)
            if ((leftElement is MathOperatorMultiply) && ((leftElement as MathOperator).LeftParam is MathVariableNumber))
            {
                MathOperator op = leftElement as MathOperator;
                leftMultiplier = (op.LeftParam as MathVariableNumber).Value;
                leftElement = op.RightParam;
            }

            if ((rightElement is MathOperatorMultiply) && ((rightElement as MathOperator).LeftParam is MathVariableNumber))
            {
                MathOperator op = rightElement as MathOperator;
                rightMultiplier = (op.LeftParam as MathVariableNumber).Value;
                rightElement = op.RightParam;
            }

            // if the subtrees are identical then we can add them
            if (leftElement.EqualsSubtree(rightElement))
            {
                // parenthesize the common term if necessary
                if (!(leftElement is MathVariable || leftElement is MathOperatorParenthesis || leftElement is MathOperatorExponent))
                {
                    MathOperatorParenthesis parenthesisNode = new MathOperatorParenthesis();
                    parenthesisNode.LeftParam = leftElement;
                    leftElement = parenthesisNode;
                }

                // based on the multiplier there are 3 cases to handle (note: multipiers can be negative numbers so the resulting combination of the terms can go negative, etc)
                float newMultiplier = leftMultiplier + rightMultiplier;

                // case 1 - multiplier is greater than 1 or less than 0, so we should replace ourselves with the common term multipied by the multiplier
                if (newMultiplier > 1.0f || newMultiplier < 0.0f)
                {
                    MathOperatorMultiply multiplierNode = new MathOperatorMultiply();
                    multiplierNode.LeftParam = new MathVariableNumber(newMultiplier);
                    multiplierNode.RightParam = leftElement;
                    currentNode = multiplierNode;
                    ParentNode.ReplaceParam(this, currentNode);
                }
                // case 2 - multiplier reduces to 1 and so we can replace ourselves with just the common term
                else if (newMultiplier == 1.0f)
                {
                    currentNode = leftElement;
                    ParentNode.ReplaceParam(this, currentNode);
                }
                // case 3 - multiplier reduces to 0 and so we can replace ourselves a number of value 0.0f
                else if (newMultiplier == 0.0f)
                {
                    MathVariableNumber numberNode = new MathVariableNumber(0.0f);
                    currentNode = numberNode;
                    ParentNode.ReplaceParam(this, currentNode);
                }
                else
                {
                    Assert.Fatal(false, "Unhandled case in Simplify_CombineIdenticalTermsInAddition()");
                }
            }

            // return to the caller whatever is now at this point in the tree (us if we weren't replaced by a number or the number if we were replaced)
            return currentNode;
        }

        public override MathFreak.Math.Views.MathElementView GenerateView(float charHeight, float spacing, Microsoft.Xna.Framework.Graphics.SpriteFont font)
        {
            return new MathOperatorAddView(this, charHeight, spacing, font);
        }
    }
}
