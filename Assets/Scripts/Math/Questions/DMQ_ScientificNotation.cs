using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;



namespace MathFreak.Math.Questions
{
    /// <summary>
    /// Player has to evaluate a term that has been given in scientific notation (x * 10^y)
    /// </summary>
    public class DMQ_ScientificNotation : DynamicMathQuestion
    {
        private const int MIN_VAR1 = -20;
        private const int MAX_VAR1 = 20;
        private const int MIN_VAR2 = -4;
        private const int MAX_VAR2 = 4;

        public override string[] GetContent()
        {
            string[] content = new string[6];

            // get the number, get the exponent
            int number = GetNonZeroRandomNumber(MIN_VAR1, MAX_VAR1 + 1, 2);
            int exponent = GetNonZeroRandomNumber(MIN_VAR2, MAX_VAR2 + 1, 2);

            // create the question
            content[0] = WrapMathString(number.ToString() + " * 10 ^ " + exponent.ToString()) + "#= ?";

            // fill in the correct answer - note: we will do this without using floating point because that can mess things up due to inaccuracies that sometimes occur in it
            content[1] = GetNumberStringFromScientificNotation(number, exponent);

            // create decoys by perturbing the exponent and calculating the new result
            content[2] = GetNumberStringFromScientificNotation(number, exponent + 1);
            content[3] = GetNumberStringFromScientificNotation(number, exponent - 1);
            content[4] = GetNumberStringFromScientificNotation(number, exponent - 2);

            // provide a hint text
            content[5] = "This is the hint for scientific notation";

            return content;
        }

        private string GetNumberStringFromScientificNotation(int number, int exponent)
        {
            bool isNegative = (number < 0); // we'll work with the number in it's non-negative form and add the negation symbol back in at the very end if required
            string numberString = isNegative ? (-number).ToString() : number.ToString();

            if (exponent < 0)
            {
                int absExponent = -exponent;

                // prefix zeros if necessary
                if (absExponent >= numberString.Length)
                {
                    numberString = (new string('0', (absExponent - numberString.Length) + 1)) + numberString;
                }

                // insert decimal point
                numberString = numberString.Insert(numberString.Length - absExponent, ".");

                numberString = numberString.TrimEnd(' ');  // trim trailing zeros
                numberString = numberString.TrimEnd('.');  // trim trailing decimal point
            }
            else
            {
                numberString = (numberString + new string('0', exponent));
            }

            if (isNegative)
            {
                return "-" + numberString;
            }
            else
            {
                return numberString;
            }
        }
    }
}
