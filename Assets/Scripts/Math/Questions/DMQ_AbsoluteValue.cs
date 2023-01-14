using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;



namespace MathFreak.Math.Questions
{
    /// <summary>
    /// Player must find the value of the expression which includes the absolute value operator, e.g.
    /// '|-8|' is 8 (can also have '-|-8|' which is -8, to spice things up a bit.
    /// </summary>
    public class DMQ_AbsoluteValue : DynamicMathQuestion
    {
        private const int MIN_VAR1 = -100;
        private const int MAX_VAR1 = 101;

        public override string[] GetContent()
        {
            string[] content = new string[6];

            // create the var and decide if going to prefix a negation operator
            int var1 = GetNonZeroRandomNumber(MIN_VAR1, MAX_VAR1, _rnd.Next(1, MAX_VAR1));
            bool prefixNeg = (_rnd.Next(2) == 0);

            // create the question and work out the answer, and also create the decoy answer (can only be one real decoy; the rest are blank)
            // NOTE: the math expression stuff doesn't support the absolute-value operator, but that is fine as it is simple to do it ourselves here.
            // NOTE: the '|' symbol is represented by the ` symbol due to a font creation goof (my fault not ahmed's as it happens)
            if (prefixNeg)
            {
                content[0] = "-`" + var1.ToString() + "`" + " = ?";
                content[1] = (-System.Math.Abs(var1)).ToString();
                content[2] = (System.Math.Abs(var1)).ToString();
                content[3] = BLANK_ANSWER;
                content[4] = BLANK_ANSWER;
            }
            else
            {
                content[0] = "`" + var1.ToString() + "`" + " = ?";
                content[1] = (System.Math.Abs(var1)).ToString();
                content[2] = (-System.Math.Abs(var1)).ToString();
                content[3] = BLANK_ANSWER;
                content[4] = BLANK_ANSWER;
            }

            // provide a hint text
            content[5] = "This is the hint for absolute values";

            return content;
        }
    }
}
