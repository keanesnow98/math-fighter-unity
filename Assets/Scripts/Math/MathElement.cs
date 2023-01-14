using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MathFreak.Math.Views;
using Microsoft.Xna.Framework.Graphics;



namespace MathFreak.Math
{
    /// <summary>
    /// This class is the base class for all elements that are stored in the internal math representation.
    /// Elements in a math expression can be variables or operators of various kinds, but they all ultimately
    /// will derive from this base class, thus making it simple for an expression to containg a list of
    /// math elements and not need to worry about which are operators and which are variables.
    /// </summary>
    public abstract class MathElement
    {
        // When new operators are created they may need to add further levels here,
        public enum EnumPemdasLevel { ExpressionRoot, Rnd, AddSubract, Multiply, Divide, Exponent, Parenthesis, Variable };

        // reference to this element's parent node in the expression tree (parent will always be an operator)
        private MathOperator _parentNode;

        public MathOperator ParentNode
        {
            get { return _parentNode; }
            set { _parentNode = value; }
        }

        // expression string to insert before this element when ApplyInsertionString() is called
        public string InsertionString;

        // whether to insert the string before or after this element
        public bool InsertStringBefore;

        // Returns the element's PEMDAS level
        public abstract EnumPemdasLevel PemdasLevel();

        // Returns a text representation of this element including any sub-elements if this
        // element is an operator.  i.e. this method should return a text representation of
        // the complete subtree from this node down.
        public abstract string TextRepresentation();

        // called by derived classes to apply any insertion that is pending
        // NOTE: this method will 'consume' the insertion string, so the insertion
        //       will no longer be pending after this method call.
        protected string ApplyInsertionString(string textRep)
        {
            // if nothing to insert then return the unadalterated text
            if (InsertionString == null) return textRep;

            // else check whether to insert before or after and return the modified text
            if (InsertStringBefore)
            {
                return (InsertionString + textRep);
            }
            else
            {
                return (textRep + InsertionString);
            }
        }

        public virtual MathElementView GenerateView(float charHeight, float spacing, SpriteFont font)
        {
            throw new NotImplementedException();
        }

        public bool EqualsSubtree(MathElement element)
        {
            return (TextRepresentation() == element.TextRepresentation());
        }
    }
}
