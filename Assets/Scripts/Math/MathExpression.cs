using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GarageGames.Torque.Core;
using MathFreak.Math.Views;
using GarageGames.Torque.T2D;
using GarageGames.Torque.Materials;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using GarageGames.Torque.XNA;



namespace MathFreak.Math
{
    /// <summary>
    /// This class is the interface to a math expression.  It stores a reference to the expression
    /// tree and has various functionality that is at a higher level than the expression tree and its
    /// nodes provide.
    /// </summary>
    public class MathExpression
    {
        // maps string tokens to operator info
        private static Dictionary<string, MathOperatorInfo> _registeredOperators;

        // Our reference to the expression tree that this MathExpression is wrapping
        private MathOperatorExpressionRoot _expressionTree;

        public MathOperatorExpressionRoot ExpressionTree
        {
            get { return _expressionTree; }
        }


        public MathExpression()
        {
            _expressionTree = new MathOperatorExpressionRoot();
        }

        public MathVariableNumber Evaluate()
        {
            return _expressionTree.Evaluate();
        }

        public string TextRepresentation()
        {
            return _expressionTree.TextRepresentation();
        }

        // Call this to parse a string into a MathExpression
        //
        // NOTE: operators must be expressed in full and all terms and operators should be
        // separated by spaces (i.e "2x" should be "2 * x").  The exception is the negation
        // operator (i.e. negation is written as in the following expression-string: "2 * -5").
        public static MathExpression Parse(string expressionString)
        {
            MathExpression expression = new MathExpression();

            expression.ParseNewExpression(expressionString);

            return expression;
        }

        // Parses the expression string and replaces the existing expression tree with the result
        //
        // NOTE: operators must be expressed in full and all terms and operators should be
        // separated by spaces (i.e "2x" should be "2 * x").  The exception is the negation
        // operator (i.e. negation is written as in the following expression-string: "2 * -5").
        public void ParseNewExpression(string expressionString)
        {
            // trim whitespace from start and end of the string (TextRepresentation() will return a string that has a trailing space and tokenizing will pick that up as a token when actually it isn't something we are interested in)
            expressionString = expressionString.Trim();

            // clear the existing tree
            _expressionTree.LeftParam = null;

            MathElement lastElement = _expressionTree;    // tracks the most recently added element in the tree (usually will be a variable, but could be an operator such as the 'root' or parentheses)

            MathVariable leftParamVar = null;    // temp store for a variable that hasn't been used (consumed by an operator) yet in the parsing the string, but will be used shortly as a left hand parameter to an operator

            // tokenize the string
            string[] tokens = expressionString.Split(' ');

            // special case if only one token (e.g. this can happen if all there is, is a single number, such as for an answer)
            if (tokens.Length == 1)
            {
                Assert.Fatal(!IsOperator(tokens[0]), "String with only one token contains an operator as that sole token - should be a number");

                MathVariable var = CreateVariable(tokens[0]);
                _expressionTree.LeftParam = var;

                return; // return early
            }

            // else parse the tokens into the math expression tree form
            for (int i = 0; i < tokens.Length; i++)
            {
                // get next token
                string token = tokens[i];

                // ignore empty tokens - this can happen if there are extra spaces in the expression string (i.e. when splitting the string on spaces, if there are two consecutive spaces we will get a "" empty string as the resulting token)
                if (token.Length == 0) continue;

                // if it's an opening parenthesis then we'll need to kick off a new subexpression
                // NOTE: this is for an opening parenthesis at the beginning of an expression or subexpression;
                //       For a parenthesis mid expression we handle that further down as the right param for an operator.
                if (token == "(")
                {
                    // we can kick off a new subexpression just by adding the parenthesis operator
                    MathElement parenthesis = new MathOperatorParenthesis();
                    (lastElement as MathOperator).RightParam = parenthesis;

                    // set the lastElement to our newly created parenthesis operator
                    lastElement = parenthesis;

                    // and the rest will take care of itself when we pick up the next tokens
                    continue;
                }
                // else if it's a closing parenthesis then we'll need to close a subexpression
                else if (token == ")")
                {
                    // go back up the tree from the lastElement until we hit a parenthesis operator
                    MathOperator searchOp;

                    if (lastElement is MathOperatorParenthesis)
                    {
                        searchOp = lastElement as MathOperator;

                        // special case: if there is a solitary variable then we seem to be enclosing it so add it as our param
                        searchOp.LeftParam = leftParamVar;
                        leftParamVar = null;
                    }
                    else
                    {
                        searchOp = lastElement.ParentNode;
                    }

                    while (!(searchOp is MathOperatorParenthesis))
                    {
                        searchOp = searchOp.ParentNode;
                    }

                    // set the lastElement to the parenthesis operator we found
                    lastElement = searchOp;

                    // the rest will take care of itself when we pick up the next tokens
                    continue;
                }
                // else determine its type (operator or variable)
                //      if it's a variable then store it temporarily (should be an operator coming up next and that will use the variable)
                else if (!IsOperator(token))
                {
                    leftParamVar = CreateVariable(token);
                    continue;
                }


                //      else it's an operator so
                //          create an instance of the correct type of operator
                MathOperator op = CreateOperator(token);

                //          get the next token (which should be a variable) and set the operator's right token to it.
                //          NOTE: ignore empty tokens - this can happen if there are extra spaces in the expression string (i.e. when splitting the string on spaces, if there are two consecutive spaces we will get a "" empty string as the resulting token)
                do
                {
                    i++;
                    token = tokens[i];

                } while (token.Length == 0);

                MathElement rightParamVar;

                // check if it's an opening parenthesis
                if (token == "(")
                {
                    // create a parenthesis operator that we will add to the tree as the right param of the current operator
                    rightParamVar = new MathOperatorParenthesis();
                }
                // else must be a variable
                else
                {
                    // create a math variable with the new token
                    rightParamVar = CreateVariable(token);
                }

                // assign the variables to the operator's params and 'consume' the left param var by setting it null
                op.LeftParam = leftParamVar;
                op.RightParam = rightParamVar;
                leftParamVar = null;

                //          if both the operator's param's are non-null then we are adding the first node in the tree
                //              or adding the first node in a subtree (due to parentheses) so just add directly to the tree
                if (op.LeftParam != null && op.RightParam != null)
                {
                    (lastElement as MathOperator).LeftParam = op;
                }
                // else we should append the operator to the tree after the lastElement
                else
                {
                    // this is an element that we will locate in the tree where we should *actually* be inserting the operator, so that we obey pemdas rules (and expression evaluation order in general)
                    MathElement targetElement;

                    // go up the tree until an operator is found with pemdas < the operator we are inserting, or until we reach a parenthesis operator (parentheses mark an independent sub-expression so we must stay within that sub-expression and not wander off outside it up the tree!)
                    targetElement = lastElement.ParentNode;
                    MathElement prevElement = lastElement;

                    while (targetElement.PemdasLevel() >= op.PemdasLevel() && !(targetElement is MathOperatorParenthesis))
                    {
                        prevElement = targetElement;
                        targetElement = targetElement.ParentNode;
                    }

                    //      retrace the search one node to get the element we found prior to the
                    //          operator we just located
                    targetElement = prevElement;

                    //      replace targetElement with our operator
                    targetElement.ParentNode.ReplaceParam(targetElement, op);

                    //      set our operator's left param to targetElement
                    op.LeftParam = targetElement;
                }

                // update the last element to reference the right param of the op we just added
                lastElement = rightParamVar;
            }
        }

        // inserts an expression string before the specified element in the math expression represented
        // by the tree the insertionPoint element is part of.  Where 'newElement' is the element to insert
        public void InsertBefore(MathElement insertionPoint, string expressionString)
        {
            // if there's no trailing space then add one
            if (expressionString[expressionString.Length - 1] != ' ')
            {
                expressionString += ' ';
            }

            // set the insertion point's insertion string to the expression string we want to insert
            insertionPoint.InsertionString = expressionString;

            // flag the insertion as 'before'
            insertionPoint.InsertStringBefore = true;

            // now get the text representation of the whole expression tree (with the insertion applied)
            // and then rebuild the tree from the new expression string that is returned.
            ParseNewExpression(_expressionTree.TextRepresentation());
        }

        // inserts this element after the specified element in the math expression represented
        // by the tree the insertionPoint element is part of.  Where 'newElement' is the element to insert
        public void InsertAfter(MathElement insertionPoint, string expressionString)
        {
            // if there's no trailing space then add one
            if (expressionString[expressionString.Length - 1] != ' ')
            {
                expressionString += ' ';
            }

            // set the insertion point's insertion string to the expression string we want to insert
            insertionPoint.InsertionString = expressionString;

            // flag the insertion as 'after'
            insertionPoint.InsertStringBefore = false;

            // now get the text representation of the whole expression tree (with the insertion applied)
            // and then rebuild the tree from the new expression string that is returned.
            ParseNewExpression(_expressionTree.TextRepresentation());
        }

        public void AdjustDivisionParamsForIntegerResult()
        {
            _expressionTree.AdjustDivisionParamsForIntegerResult();
            ParseNewExpression(_expressionTree.TextRepresentation());   // make sure everything is kosha
        }

        // returns true if the string token maps to a registered operator
        public static bool IsOperator(string token)
        {
            return _registeredOperators.ContainsKey(token);
        }

        // Creates a MathOperator derived instance based on the operator that the string token maps to
        // Returns null if the string token does not map to a registered operator.
        public static MathOperator CreateOperator(string token)
        {
            MathOperatorInfo info;

            if (!_registeredOperators.TryGetValue(token, out info))
            {
                Assert.Fatal(false, "MathExpression::CreateOperator() - unrecognised operator string token: " + token);
                return null;
            }

            return info.CreateInstance();
        }

        // Creates a MathVariable of the appropriate type depending on the content of the string token.
        public static MathVariable CreateVariable(string token)
        {
            // detect if number or letter - check the last character as letter variables must not contain numbers
            char lastChar = token[token.Length - 1];

            if (lastChar >= '0' && lastChar <= '9')
            {
                // its a number so create and return a MathVariableNumber
                return new MathVariableNumber(float.Parse(token));
            }
            else
            {
                // else we assume it's letter variable

                // check for negation and then create and return the MathVariableLetter instance
                if (token[0] == '-')
                {
                    return new MathVariableLetter(true, token[1]);
                }
                else
                {
                    return new MathVariableLetter(false, token[0]);
                }
            }
        }

        // register an operator to map it to a string token so that the operator can be recognised
        // when parsing expression strings
        public static void RegisterOperator(string token, MathOperatorInfo elementInfo)
        {
            _registeredOperators.Add(token, elementInfo);
        }

        // should be called once on game startup to register the default math elements
        public static void RegisterDefaultOperators()
        {
            _registeredOperators = new Dictionary<string, MathOperatorInfo>();

            RegisterOperator("+", new MathOperatorInfo(delegate()
                    {
                        return new MathOperatorAdd();
                    }));

            RegisterOperator("-", new MathOperatorInfo(delegate()
                    {
                        return new MathOperatorSubtract();
                    }));

            RegisterOperator("/", new MathOperatorInfo(delegate()
                    {
                        return new MathOperatorDivide(MathOperatorDivide.EnumDisplayType.Fraction);
                    }));

            RegisterOperator("//", new MathOperatorInfo(delegate()
                    {
                        return new MathOperatorDivide(MathOperatorDivide.EnumDisplayType.DivSign);
                    }));

            RegisterOperator("///", new MathOperatorInfo(delegate()
                    {
                        return new MathOperatorDivide(MathOperatorDivide.EnumDisplayType.LongDiv);
                    }));

            RegisterOperator("*", new MathOperatorInfo(delegate()
                    {
                        return new MathOperatorMultiply();
                    }));

            RegisterOperator("^", new MathOperatorInfo(delegate()
                    {
                        return new MathOperatorExponent(MathOperatorExponent.EnumType.Power);
                    }));

            RegisterOperator("ROOT", new MathOperatorInfo(delegate()
                {
                    return new MathOperatorExponent(MathOperatorExponent.EnumType.Root);
                }));

            RegisterOperator("RND", new MathOperatorInfo(delegate()
                    {
                        return new MathOperatorRandom();
                    }));
        }

        // returns a Texture2D that is sized and textured with a visual representation of the
        // math expression, using the provided font and character height parameters, and spacing parameter.
        public LogicalRenderTexture GenerateTexture(float charHeight, float spacing, SpriteFont font, Vector4 backgroundColour)
        {
            // set some rendering parameters (just surfaceformat to set at the moment)
            //MathElementView.InitializeRenderSettings(backgroundColour);

            // generate the visual representation of the math expression
            MathElementView view = _expressionTree.GenerateView(charHeight, spacing, font);

            // if a coloured background was requested then render the texture onto a background with the requested colour
            if (backgroundColour.Z != 0.0f)  // i.e. test for alpha being non-zero
            {
                LogicalRenderTexture expressionTexture = view.Texture;

                // create a render target
                LogicalRenderTexture lrt = LRTPool.Instance.AcquireLRT(expressionTexture.Width, expressionTexture.Height);

                // setup the device stuff
                TorqueEngineComponent.Instance.Game.GraphicsDevice.SetRenderTarget(0, lrt.RenderTarget);
                TorqueEngineComponent.Instance.Game.GraphicsDevice.Clear(ClearOptions.Target, backgroundColour, 0.0f, 0);

                // setup the spritebatch
                SpriteBatch spriteBatch = Game.SpriteBatch;

                spriteBatch.Begin();
                spriteBatch.Draw(expressionTexture.Texture, Vector2.Zero, expressionTexture.Region, Color.White);
                spriteBatch.End();

                // reset device stuff
                TorqueEngineComponent.Instance.Game.GraphicsDevice.SetRenderTarget(0, null);

                // cleanup
                LRTPool.Instance.ReleaseLRT(expressionTexture);

                // resolve and return the newly rendered texture
                lrt.ResolveTexture();
                return lrt;
            }
            // else just return the expression texture as-is
            else
            {
                return view.Texture;
            }
        }

        // will evaluate any numerical sub-expressions and replace those sub-expressions with their numerical equivalent
        public void Simplify_EvaluateNumericalExpressions()
        {
            _expressionTree.Simplify_EvaluateNumericalExpressions();
        }

        public void Simplify_MoveNumbersToLeftOfMultiplicationExpressions()
        {
            _expressionTree.Simplify_MoveNumbersToLeftOfMultiplicationExpressions();
        }

        // if two identical terms are mutliplied then their powers are added
        // e.g. "x * x" becomes "x ^ 2" and "x ^ 2 * x ^ 3" becomes "x ^ 5"
        public void Simplify_AddExponents()
        {
            _expressionTree.Simplify_AddExponents();
        }

        // if two identical terms are divided, one by the other, then their powers are subtracted
        // e.g. "x / x" becomes "1 aka x^0" and "x^4 / x^2" becomes "x^2"
        public void Simplify_SubtractExponents()
        {
            _expressionTree.Simplify_SubtractExponents();
        }

        //// if two letter variables are the same but one is negated then they cancel out
        //// e.g. "x + -x" becomes "0"
        //public void Simplify_CancelIdenticalLetterVariablesInAddition()
        //{
        //    _expressionTree.Simplify_CancelIdenticalLetterVariablesInAddition();
        //}

        // identical terms can be combined - this includes terms that have a multiplier modifying them
        // e.g. "x - x" becomes "0" and "4 * x + 2 * x" becomes "2 * x"
        public void Simplify_CombineIdenticalTermsInSubtraction()
        {
            _expressionTree.Simplify_CombineIdenticalTermsInSubtraction();
        }

        // identical terms can be combined - this includes terms that have a multiplier modifying them
        // e.g. "x + x" becomes "2 * x" and "2 * x + 4 * x" becomes "6 * x"
        public void Simplify_CombineIdenticalTermsInAddition()
        {
            _expressionTree.Simplify_CombineIdenticalTermsInAddition();
        }

        public class MathOperatorInfo
        {
            private CreateInstanceDelegate _createInstance;

            public CreateInstanceDelegate CreateInstance
            {
                get { return _createInstance; }
            }

            public delegate MathOperator CreateInstanceDelegate();

            public MathOperatorInfo(CreateInstanceDelegate createInstanceDelegate)
            {
                _createInstance = createInstanceDelegate;
            }
        }
    }
}
