using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MathFreak.Math.Views;
using GarageGames.Torque.Core;

namespace MathFreak.Math
{
    /// <summary>
    /// Represents parentheses the internal math representation stuff.
    /// 
    /// NOTE: there is no open/close pair of operators as the math representation is stored
    /// in an expression tree so parentheses is just a node with it's contents as a sub-tree
    /// under it.
    /// </summary>
    public class MathOperatorParenthesis : MathOperator
    {
        public override MathElement LeftParam
        {
            get
            {
                return base.RightParam;
            }
            set
            {
                base.RightParam = value;
            }
        }

        public override EnumPemdasLevel PemdasLevel()
        {
            return EnumPemdasLevel.Parenthesis;
        }

        public override string TextRepresentation()
        {
            return ApplyInsertionString("( " + LeftParam.TextRepresentation() + ") ");
        }

        public override MathVariableNumber Evaluate()
        {
            return EvaluateLeftParam();
        }

        public override MathElement Simplify_EvaluateNumericalExpressions()
        {
            MathElement currentNode = this;

            // simplify our param first
            MathElement leftElement = LeftParam;

            if (leftElement is MathOperator)
            {
                leftElement = (leftElement as MathOperator).Simplify_EvaluateNumericalExpressions();
            }

            // if our param is a number then replace ourselves with it in the tree
            if (leftElement is MathVariableNumber)
            {
                currentNode = leftElement;
                ParentNode.ReplaceParam(this, currentNode);
            }

            // return to the caller whatever is now at this point in the tree (us if we weren't replaced by a number or the number if we were replaced)
            return currentNode;
        }

        public override void Simplify_MoveNumbersToLeftOfMultiplicationExpressions()
        {
            MathElement leftElement = LeftParam;

            if (leftElement is MathOperator)
            {
                (leftElement as MathOperator).Simplify_MoveNumbersToLeftOfMultiplicationExpressions();
            }
        }

        public override MathElement Simplify_SubtractExponents()
        {
            MathElement leftElement = LeftParam;

            if (leftElement is MathOperator)
            {
                leftElement = (leftElement as MathOperator).Simplify_SubtractExponents();
            }

            return this;
        }

        //public override MathElement Simplify_CancelIdenticalLetterVariablesInAddition()
        //{
        //    MathElement leftElement = LeftParam;

        //    if (leftElement is MathOperator)
        //    {
        //        leftElement = (leftElement as MathOperator).Simplify_CancelIdenticalLetterVariablesInAddition();
        //    }

        //    return this;
        //}

        public override MathElement Simplify_CombineIdenticalTermsInSubtraction()
        {
            MathElement leftElement = LeftParam;

            if (leftElement is MathOperator)
            {
                leftElement = (leftElement as MathOperator).Simplify_CombineIdenticalTermsInSubtraction();
            }

            return this;
        }

        public override MathElement Simplify_CombineIdenticalTermsInAddition()
        {
            MathElement leftElement = LeftParam;

            if (leftElement is MathOperator)
            {
                leftElement = (leftElement as MathOperator).Simplify_CombineIdenticalTermsInAddition();
            }

            return this;
        }

        public override MathFreak.Math.Views.MathElementView GenerateView(float charHeight, float spacing, Microsoft.Xna.Framework.Graphics.SpriteFont font)
        {
            // to display or not to display the parenthesis around the subexpression?

            // ...don't display them
            if (ParentNode is MathOperatorDivide)
            {
                return new MathOperatorParenthesesInvisibleView(this, charHeight, spacing, font);
            }
            // ...display them
            else
            {
                return new MathOperatorParenthesesView(this, charHeight, spacing, font);
            }
        }
    }
}
