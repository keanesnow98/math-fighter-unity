using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MathFreak.Math.Views;
using GarageGames.Torque.Core;

namespace MathFreak.Math
{
    /// <summary>
    /// Represents the multiplication operator in the internal math representation stuff.
    /// </summary>
    public class MathOperatorMultiply : MathOperator
    {
        public override EnumPemdasLevel PemdasLevel()
        {
            return EnumPemdasLevel.Multiply;
        }

        public override string TextRepresentation()
        {
            return LeftParam.TextRepresentation() + ApplyInsertionString("* ") + RightParam.TextRepresentation();
        }

        public override MathVariableNumber Evaluate()
        {
            MathVariableNumber leftVar = EvaluateLeftParam();
            MathVariableNumber rightVar = EvaluateRightParam();

            // multiplying by zero evaluates to zero
            if ((leftVar != null && leftVar.Value == 0.0f) || (rightVar != null && rightVar.Value == 0.0f))
            {
                return new MathVariableNumber(0.0f);
            }

            // if one or other param is not a number then we can't evaluate them into a number
            if (leftVar == null) return null;
            if (rightVar == null) return null;

            // evaluate to a number and return the result
            return new MathVariableNumber((leftVar as MathVariableNumber).Value * (rightVar as MathVariableNumber).Value);
        }

        public override MathElement Simplify_EvaluateNumericalExpressions()
        {
            MathElement currentNode = this;

            // simplify our params first
            MathElement leftElement = LeftParam;
            MathElement rightElement = RightParam;

            if (leftElement is MathOperator)
            {
                leftElement = (leftElement as MathOperator).Simplify_EvaluateNumericalExpressions();
            }

            if (rightElement is MathOperator)
            {
                rightElement = (rightElement as MathOperator).Simplify_EvaluateNumericalExpressions();
            }

            // now evaluate ourselves
            // ...special case - if multipying by '1' then don't need the '1'
            if (!(rightElement is MathVariableNumber) && (leftElement is MathVariableNumber) && ((leftElement as MathVariableNumber).Value == 1.0f))
            {
                currentNode = rightElement;
                ParentNode.ReplaceParam(this, currentNode);
            }
            else if (!(leftElement is MathVariableNumber) && (rightElement is MathVariableNumber) && ((rightElement as MathVariableNumber).Value == 1.0f))
            {
                currentNode = leftElement;
                ParentNode.ReplaceParam(this, currentNode);
            }
            //// ...special case - if multiplying by -1 then don't need the '1' (but do need the '-')
            //else if ((rightElement is MathVariableNumber) && ((rightElement as MathVariableNumber).Value == -1.0f))
            //{
            //    // TODO - do this bit when negative letters variables are properly supported
            //}
            // ...use the normal evaluation method
            else
            {
                currentNode = Evaluate();

                if (currentNode != null)
                {
                    ParentNode.ReplaceParam(this, currentNode);
                }
            }

            // return to the caller whatever is now at this point in the tree (us if we weren't replaced by a number or the number if we were replaced)
            return currentNode;
        }

        public override void Simplify_MoveNumbersToLeftOfMultiplicationExpressions()
        {
            base.Simplify_MoveNumbersToLeftOfMultiplicationExpressions();

            // do the moving of a number from right to left side of the multiplication expression if necessary
            MathElement rightElement = RightParam;

            if (rightElement is MathVariableNumber)
            {
                MathElement leftElement = LeftParam;

                // simple case - letter or parenthesis
                if (leftElement is MathVariableLetter || leftElement is MathOperatorParenthesis)
                {
                    // swap 'em
                    LeftParam = rightElement;
                    RightParam = leftElement;
                }
                // more complex case - the term has been raised to a power
                else if (leftElement is MathOperatorExponent && ((leftElement as MathOperator).LeftParam is MathVariableLetter || (leftElement as MathOperator).LeftParam is MathOperatorParenthesis))
                {
                    // swap 'em
                    LeftParam = rightElement;
                    RightParam = leftElement;
                }
            }
        }

        public override MathElement Simplify_AddExponents()
        {
            MathElement currentNode = this;

            // simplify our params first
            MathElement leftElement = LeftParam;
            MathElement rightElement = RightParam;

            if (leftElement is MathOperator)
            {
                leftElement = (leftElement as MathOperator).Simplify_AddExponents();
            }

            if (rightElement is MathOperator)
            {
                rightElement = (rightElement as MathOperator).Simplify_AddExponents();
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

            // if the subtrees are identical then we can combine the exponents
            if (leftElement.EqualsSubtree(rightElement))
            {
                // parenthesize the common term if necessary
                if (!(leftElement is MathVariable || leftElement is MathOperatorParenthesis))
                {
                    MathOperatorParenthesis parenthesisNode = new MathOperatorParenthesis();
                    parenthesisNode.LeftParam = leftElement;
                    leftElement = parenthesisNode;
                }

                // create and setup the exponent node
                MathOperatorExponent exponentNode = new MathOperatorExponent();
                exponentNode.LeftParam = leftElement;
                exponentNode.RightParam = new MathVariableNumber(leftExponent + rightExponent);

                // replace ourselves with the new exponent node
                currentNode = exponentNode;
                ParentNode.ReplaceParam(this, currentNode);
            }

            // return to the caller whatever is now at this point in the tree (us if we weren't replaced by a number or the number if we were replaced)
            return currentNode;
        }

        public override MathFreak.Math.Views.MathElementView GenerateView(float charHeight, float spacing, Microsoft.Xna.Framework.Graphics.SpriteFont font)
        {
            //  the right param is parenthesis
            if (RightParam is MathOperatorParenthesis)
            {
                return new MathOperatorMultiplyViewShort(this, charHeight, spacing, font);
            }
            // if the left param is a number and the right param is a letter
            else if (LeftParam is MathVariableNumber)
            {
                // simple case - right param is a letter
                if (RightParam is MathVariableLetter)
                {
                    return new MathOperatorMultiplyViewShort(this, charHeight, spacing, font);
                }
                // more complex case - term is raised to a power
                else if (RightParam is MathOperatorExponent && ((RightParam as MathOperator).LeftParam is MathVariableLetter || (RightParam as MathOperator).LeftParam is MathOperatorParenthesis))
                {
                    return new MathOperatorMultiplyViewShort(this, charHeight, spacing, font);
                }
            }
        
            // else we default to the standard form
            return new MathOperatorMultiplyView(this, charHeight, spacing, font);
        }
    }
}
