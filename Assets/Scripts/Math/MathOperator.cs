using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GarageGames.Torque.Core;

namespace MathFreak.Math
{
    /// <summary>
    /// Base class for all operators used in the internal math representation.
    /// Linked together operators and variables form an expression tree (the math expression represented
    /// in the form of a tree data structure).
    /// 
    /// NOTE: unlike variables, operators are not immutable once created - their parameters can be changed
    /// after the operator instance has been created (this better facilitates faster manipulation of the
    /// expression tree).
    /// </summary>
    public abstract class MathOperator : MathElement
    {
        // Basically we will have binary operators aside from the odd unary one - unary operators
        // will be able to overload functionality to deal with left/right param stuff however they
        // require.
        private MathElement _leftParam;
        private MathElement _rightParam;

        // Get/Set the operator's left param - this is virtual so that unary operators
        // can handle this however they need to.
        public virtual MathElement LeftParam
        {
            get { return _leftParam; }

            set
            {
                _leftParam = value;

                if (value != null)
                {
                    value.ParentNode = this;
                }
            }
        }

        // Get/Set the operator's right param - this is virtual so that unary operators
        // can handle this however they need to.
        public virtual MathElement RightParam
        {
            get { return _rightParam; }


            set
            {
                _rightParam = value;

                if (value != null)
                {
                    value.ParentNode = this;
                }
            }
        }


        // Evaluates the expression subtree that the operator is the root of
        public abstract MathVariableNumber Evaluate();

        public virtual void AdjustDivisionParamsForIntegerResult()
        {
            MathElement leftElement = LeftParam;
            MathElement rightElement = RightParam;

            if (leftElement != null && leftElement is MathOperator)
            {
                (leftElement as MathOperator).AdjustDivisionParamsForIntegerResult();
            }

            if (rightElement != null && rightElement != leftElement && rightElement is MathOperator)
            {
                (rightElement as MathOperator).AdjustDivisionParamsForIntegerResult();
            }
        }

        public virtual MathElement Simplify_EvaluateNumericalExpressions()
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
            currentNode = Evaluate();

            if (currentNode != null)
            {
                ParentNode.ReplaceParam(this, currentNode);
            }

            // return to the caller whatever is now at this point in the tree (us if we weren't replaced by a number or the number if we were replaced)
            return currentNode;
        }

        // e.g. x * 2 should be displayed as 2x so the number needs to be on the left side to be displayed
        // properly
        public virtual void Simplify_MoveNumbersToLeftOfMultiplicationExpressions()
        {
            MathElement leftElement = LeftParam;
            MathElement rightElement = RightParam;

            if (leftElement is MathOperator)
            {
                (leftElement as MathOperator).Simplify_MoveNumbersToLeftOfMultiplicationExpressions();
            }

            if (rightElement is MathOperator)
            {
                (rightElement as MathOperator).Simplify_MoveNumbersToLeftOfMultiplicationExpressions();
            }
        }

        public virtual MathElement Simplify_AddExponents()
        {
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

            return this;
        }

        public virtual MathElement Simplify_SubtractExponents()
        {
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

            return this;
        }

        //public virtual MathElement Simplify_CancelIdenticalLetterVariablesInAddition()
        //{
        //    MathElement leftElement = LeftParam;
        //    MathElement rightElement = RightParam;

        //    if (leftElement is MathOperator)
        //    {
        //        leftElement = (leftElement as MathOperator).Simplify_CancelIdenticalLetterVariablesInAddition();
        //    }

        //    if (rightElement is MathOperator)
        //    {
        //        rightElement = (rightElement as MathOperator).Simplify_CancelIdenticalLetterVariablesInAddition();
        //    }

        //    return this;
        //}

        public virtual MathElement Simplify_CombineIdenticalTermsInSubtraction()
        {
            MathElement leftElement = LeftParam;
            MathElement rightElement = RightParam;

            if (leftElement is MathOperator)
            {
                leftElement = (leftElement as MathOperator).Simplify_CombineIdenticalTermsInSubtraction();
            }

            if (rightElement is MathOperator)
            {
                rightElement = (rightElement as MathOperator).Simplify_CombineIdenticalTermsInSubtraction();
            }

            return this;
        }

        public virtual MathElement Simplify_CombineIdenticalTermsInAddition()
        {
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

            return this;
        }

        protected MathVariableNumber EvaluateLeftParam()
        {
            if (LeftParam != null)
            {
                if (LeftParam is MathVariableNumber)
                {
                    return (LeftParam as MathVariableNumber);
                }
                else if (LeftParam is MathOperator)
                {
                    return (LeftParam as MathOperator).Evaluate();
                }
            }

            return null;
        }

        protected MathVariableNumber EvaluateRightParam()
        {
            if (RightParam != null)
            {
                if (RightParam is MathVariableNumber)
                {
                    return (RightParam as MathVariableNumber);
                }
                else if (RightParam is MathOperator)
                {
                    return (RightParam as MathOperator).Evaluate();
                }
            }

            return null;
        }

        // Replaces the parameter that has the 'search' value, with the 'replace' element
        // Returns true if replacement succeeded
        public virtual bool ReplaceParam(MathElement search, MathElement replace)
        {
            if (LeftParam == search)
            {
                LeftParam = replace;
                return true;
            }

            if (RightParam == search)
            {
                RightParam = replace;
                return true;
            }

            Assert.Fatal(false, "MathOperator::ReplaceParam() - Failed to replace element [" + search + "] with element [" + replace + "]");

            return false;
        }
    }
}
