using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GarageGames.Torque.Core;
using MathFreak.Math.Views;
using Microsoft.Xna.Framework.Graphics;



namespace MathFreak.Math
{
    /// <summary>
    /// Math expressions are stored as a tree stucture of operators and variables.
    /// For any math expression an instance of MathOperatorExpressionRoot will be the
    /// root of the expression tree, providing a fixed point of access into the tree
    /// (this is necessary because some operations on the tree would result in nodes
    /// being added *above* the root node of the tree if there was not a special node
    /// with ultra low pemdas level to prevent it).
    /// </summary>
    public class MathOperatorExpressionRoot : MathOperator
    {
        public override MathElement RightParam
        {
            get
            {
                return base.LeftParam;
            }
            set
            {
                base.LeftParam = value;
            }
        }

        public MathOperatorExpressionRoot()
        {
        }

        public override EnumPemdasLevel PemdasLevel()
        {
            return EnumPemdasLevel.ExpressionRoot;
        }

        public override string TextRepresentation()
        {
            // NOTE: this operator does not make a call to apply an insertion string as that does not make sense for the tree root
            return LeftParam.TextRepresentation();
        }

        public override MathVariableNumber Evaluate()
        {
            return EvaluateLeftParam();
        }

        public override MathElement Simplify_EvaluateNumericalExpressions()
        {
            // simplify our param
            if (LeftParam is MathOperator)
            {
                (LeftParam as MathOperator).Simplify_EvaluateNumericalExpressions();
            }

            // we can't be replaced in the tree as we are the root of the expression, so just return ourselves to the caller
            return this;
        }

        public override void Simplify_MoveNumbersToLeftOfMultiplicationExpressions()
        {
            MathElement leftElement = LeftParam;

            if (leftElement is MathOperator)
            {
                (leftElement as MathOperator).Simplify_MoveNumbersToLeftOfMultiplicationExpressions();
            }
        }

        public override MathElement Simplify_AddExponents()
        {
            MathElement leftElement = LeftParam;

            if (leftElement is MathOperator)
            {
                leftElement = (leftElement as MathOperator).Simplify_AddExponents();
            }

            return this;
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

        public override MathFreak.Math.Views.MathElementView GenerateView(float h, float spacing, SpriteFont font)
        {
            return new MathOperatorExpressionRootView(this, h, spacing, font);
        }
    }
}
