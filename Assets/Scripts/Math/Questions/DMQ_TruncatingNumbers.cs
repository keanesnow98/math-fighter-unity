using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;



namespace MathFreak.Math.Questions
{
    /// <summary>
    /// Player must truncate the number to N decimal places
    /// </summary>
    public class DMQ_TruncatingNumbers : DynamicMathQuestion
    {
        private const int MIN_VAR1 = 0;
        private const int MAX_VAR1 = 50;


        // NOTE: Math.Truncate() will not truncate to an arbitrary number of decimal places so we
        // are simple using the same approach as DMQ_RoundingNumbers uses, but truncating instead of
        // rounding.
        //
        // Also Note: we are not relying on the floating point math to do stuff as it messes with stuff
        // rounding it sometimes, etc, which we definitely don't want in this instance.
        public override string[] GetContent()
        {
            string[] content = new string[6];

            // create the integer portion of the question
            int questionIntegerNumber = _rnd.Next(MIN_VAR1, MAX_VAR1);

            // decide how many decimal places should be in the answer
            int decimalPlacesInAnswer = _rnd.Next(1, 3);
            //Debug.WriteLine("decimalPlacesInAnswer: " + decimalPlacesInAnswer);

            // create a string of this many digits less one and add a 1-9 on the end (must end in non-zero)
            string decimalDigitsPart1 = "";

            for (int i = 0; i < (decimalPlacesInAnswer - 1); i++)
            {
                decimalDigitsPart1 += _rnd.Next(0, 10).ToString();
            }

            decimalDigitsPart1 += _rnd.Next(1, 10).ToString();
            //Debug.WriteLine("decimalDigitsPart1: " + decimalDigitsPart1);

            // add some decimal places for the question so that it will need truncating
            int extraDecimalPlacesForQuestion = _rnd.Next(2, 3);
            //Debug.WriteLine("extraDecimalPlacesForQuestion: " + extraDecimalPlacesForQuestion);

            // create a string of this many digits (again less one)
            string decimalDigitsPart2 = "";

            for (int i = 0; i < (extraDecimalPlacesForQuestion - 1); i++)
            {
                decimalDigitsPart2 += _rnd.Next(0, 10).ToString();
            }

            decimalDigitsPart2 += _rnd.Next(1, 10).ToString();
            //Debug.WriteLine("decimalDigitsPart2: " + decimalDigitsPart2);

            // combine the decimal parts
            string decimalPart = decimalDigitsPart1 + decimalDigitsPart2;

            // combine the integer portion of the question with the decimal part to give the number to use in the question
            content[0] = "Truncate " + questionIntegerNumber + "." + decimalPart + " to " + decimalPlacesInAnswer + " decimal places";

            // combine the integer portion with the first decimal part to give the answer to the question
            content[1] = questionIntegerNumber.ToString() + "." + decimalDigitsPart1;

            // similarly, create the decoys by removing or adding digits to the first decimal part - note: we *know* that the extra decimal places will be at least 2 so we can safely use +2 as one of out decoys (same for the part 1 decimals being at least 1 decimal place)
            if (decimalPlacesInAnswer > 1)
            {
                content[2] = questionIntegerNumber.ToString() + "." + decimalDigitsPart1.Substring(0, decimalPlacesInAnswer - 1);
            }
            else
            {
                content[2] = questionIntegerNumber.ToString();
            }

            content[3] = questionIntegerNumber.ToString() + "." + decimalDigitsPart1 + decimalDigitsPart2.Substring(0, 1);
            content[4] = questionIntegerNumber.ToString() + "." + decimalDigitsPart1 + decimalDigitsPart2.Substring(0, 2);

            // provide a hint text
            content[5] = "This is the hint for truncating numbers";

            return content;
        }
    }
}
