using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;



namespace MathFreak.Math.Questions
{
    /// <summary>
    /// Player must round the number to N decimal places
    /// </summary>
    public class DMQ_RoundingNumbers : DynamicMathQuestion
    {
        private const int MIN_VAR1 = 1;
        private const int MAX_VAR1 = 50;

        // NOTE: this is done the 'hard' way rather than using Math.Round() because we want to be sure
        // the number will always round to the N decimal places that we choose; so we need to build up the
        // components of the number ourselves or we won't have the garrauntees that we want.
        public override string[] GetContent()
        {
            string[] content = new string[6];

            // create the integer portion of the question
            int questionIntegerNumber = _rnd.Next(MIN_VAR1, MAX_VAR1);

            // decide how many decimal places should be in the answer
            int decimalPlacesInAnswer = _rnd.Next(1, 5);
            //Debug.WriteLine("decimalPlacesInAnswer: " + decimalPlacesInAnswer);

            // create a string of this many digits less one and add a 1-8 on the end (must end in non-zero and also cannot be 9 or it would mess up the 'round to n decimal places' thing if we end up rounding 'up')
            string decimalDigitsPart1 = "";

            for (int i = 0; i < (decimalPlacesInAnswer - 1); i++)
            {
                decimalDigitsPart1 += _rnd.Next(0, 10).ToString();
            }

            decimalDigitsPart1 += _rnd.Next(1, 9).ToString();
            //Debug.WriteLine("decimalDigitsPart1: " + decimalDigitsPart1);

            // add some decimal places for the question so that it will need rounding
            int extraDecimalPlacesForQuestion = _rnd.Next(1, 4);
            //Debug.WriteLine("extraDecimalPlacesForQuestion: " + extraDecimalPlacesForQuestion);

            // create a string of this many digits (again less one, but this time with 1-9 on the end)
            string decimalDigitsPart2 = "";

            for (int i = 0; i < (extraDecimalPlacesForQuestion - 1); i++)
            {
                decimalDigitsPart2 += _rnd.Next(0, 10).ToString();
            }

            decimalDigitsPart2 += _rnd.Next(1, 10).ToString();
            //Debug.WriteLine("decimalDigitsPart2: " + decimalDigitsPart2);

            // combine the integer portion of the question with the two decimal parts to give the number to use in the question
            float numberToPresentAsQuestion = (float)questionIntegerNumber + ((float)(int.Parse(decimalDigitsPart1 + decimalDigitsPart2))) / (float)System.Math.Pow(10, decimalPlacesInAnswer + extraDecimalPlacesForQuestion);
            //Debug.WriteLine("numberToPresentAsQuestion: " + numberToPresentAsQuestion);

            // create a number by combining the two strings separated by a decimal point
            // and round this number
            float intermediateNumberToRound = (float)int.Parse(decimalDigitsPart1) + ((float)int.Parse(decimalDigitsPart2)) / (float)System.Math.Pow(10, extraDecimalPlacesForQuestion);
            //Debug.WriteLine("intermediateNumberToRound: " + intermediateNumberToRound);

            float intermediateNumberRounded = (float)System.Math.Round(intermediateNumberToRound);
            //Debug.WriteLine("intermediateNumberRounded: " + intermediateNumberRounded);

            // now divide the number by 10^n where n is the number of decimal places to round to
            // and add the result to the integer portion of the question - the result is the answer to the question
            float answer = (float)questionIntegerNumber + (intermediateNumberRounded / (float)System.Math.Pow(10, decimalPlacesInAnswer));
            //Debug.WriteLine("answer: " + answer);

            // for the decoy answer (there can be only one) use either ceiling or floor (which ever gives the different answer from the actual answer)
            float decoyAnswer;

            if ((float)System.Math.Floor(intermediateNumberToRound) == intermediateNumberRounded)
            {
                decoyAnswer = (float)questionIntegerNumber + ((float)System.Math.Ceiling(intermediateNumberToRound) / (float)System.Math.Pow(10, decimalPlacesInAnswer));
            }
            else
            {
                decoyAnswer = (float)questionIntegerNumber + ((float)System.Math.Floor(intermediateNumberToRound) / (float)System.Math.Pow(10, decimalPlacesInAnswer));
            }

            //Debug.WriteLine("decoyAnswer: " + decoyAnswer);

            // fill in the content
            content[0] = "Round " + numberToPresentAsQuestion + " to " + decimalPlacesInAnswer + " decimal places";
            content[1] = answer.ToString();
            content[2] = decoyAnswer.ToString();
            content[3] = " ";
            content[4] = " ";

            // provide a hint text
            content[5] = "This is the hint for rounding numbers";

            return content;
        }
    }
}
