using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GarageGames.Torque.Core;



namespace MathFreak.Math.Questions
{
    /// <summary>
    /// This is the base class for dynamic math questions.  These are questions that will dynamically
    /// generate the question and answers.
    /// </summary>
    public abstract class DynamicMathQuestion : MathQuestion
    {
        protected Random _rnd;

        protected const int NUM_PRIMES = 27;
        protected static int[] PRIMES = new int[NUM_PRIMES] { 1, 2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31, 37, 41, 43, 47, 53, 59, 61, 67, 71, 73, 79, 83, 89, 97, 101 };


        public DynamicMathQuestion()
        {
            _rnd = new Random();
        }

        /// <summary>
        /// Returns a string representation of 'value' truncated to the specified number of significant figures
        /// </summary>
        /// <returns></returns>
        protected string GetStringWithSignificantFigures(float value, int sigfigs)
        {
            // parse to a string and process the string
            string valueString = value.ToString();

            // ...get everything up to the decimal point as the integer part
            string resultString = "";
            int indx = 0;

            if (valueString[0] == '-')
            {
                resultString = "-";
                indx++;
            }

            while (indx < valueString.Length && Char.IsNumber(valueString, indx))
            {
                resultString += valueString[indx];
                indx++;
            }

            // ...if there is a decimal component then we need to parse the string some more
            if (indx < valueString.Length)
            {
                int indxToStopAt = indx + sigfigs + 1;   // +1 so that we pick up the decimal point

                while (indx < valueString.Length && indx < indxToStopAt)
                {
                    resultString += valueString[indx];
                    indx++;
                }

                // trim any trailing zeros from the end
                resultString.TrimEnd('0');

                // if the decimal part is empty then we can chop off the decimal point
                if (!Char.IsNumber(valueString, valueString.Length - 1))
                {
                    resultString.Substring(0, resultString.Length - 1);
                }
            }

            return resultString;
        }

        protected int GetNonZeroRandomNumber(int min, int max, int defaultNumber)
        {
            int ret = _rnd.Next(min, max);

            if (ret == 0)
            {
                return defaultNumber;
            }
            else
            {
                return ret;
            }
        }

        // note: uses the precoded primes list at the top of this file, so the largest prime
        // that will be tried is 101
        protected int FindLargestPrimeFactorOf(int value)
        {
            int primeIndex = NUM_PRIMES - 1;

            while (primeIndex >= 0 && (value % PRIMES[primeIndex] != 0))
            {
                primeIndex--;
            }

            return PRIMES[primeIndex];
        }

        protected int FindLowestCommonDenominator(int denominator1, int denominator2)
        {
            // use the GCD to calculate the LCD
            return (denominator1 * denominator2) / Util.FindGCD(denominator1, denominator2);
        }

        protected void ReduceFractionToSimplestForm(ref int numerator, ref int denominator)
        {
            // use the GCD to calculate reduce the fraction to its simplest form
            int gcd = Util.FindGCD(numerator, denominator);
            numerator = numerator / gcd;
            denominator = denominator / gcd;
        }

        // perturbs the number by in the range lowerPert-upperPert and within the min/max final value allowes
        protected int PerturbNumber(int value, int lowerPert, int upperPert, int min, int max)
        {
            int newValue;

            do
            {
                newValue = value + _rnd.Next(lowerPert, upperPert + 1);

            } while (newValue < min || newValue > max || newValue == value);

            return newValue;
        }

        // returns a prime number between the inclusive minimum and exclusive maximum
        // note: min should be >= 1 and max should be > min
        protected int RandomPrime(int min, int max)
        {
            Assert.Fatal(min >= 1 && max > min, "RandomPrime, min/max invalid values: " + min + "/" + max);

            // get the min/max indexes into the of prime numbers to choose from
            int minIndx = 0;
            int maxIndx = NUM_PRIMES - 1;

            for (int i = 0; i < NUM_PRIMES; i++)
            {
                if (PRIMES[i] <= min)
                {
                    minIndx = i;
                }

                if (PRIMES[i] < max)
                {
                    maxIndx = i;
                }
            }

            // silliness check
            if (maxIndx < minIndx) maxIndx = minIndx;

            return PRIMES[_rnd.Next(minIndx, maxIndx + 1)];
        }



        /// <summary>
        /// Encapsulates templating of question/answer for questions where the answer must be
        /// an integer.
        /// </summary>
        public class QuestionAnswerIntegerTemplate
        {
            private string _questionTemplate;
            private int _answerIndx;
            private string[] _varsCalc;
            private string _customInfo;

            private string _question;
            private string[] _vars;

            public string Question
            {
                get { return _question; }
            }

            public string Answer
            {
                get { return _vars[_answerIndx]; }
            }

            public string[] Vars
            {
                get { return _vars; }
            }

            public string CustomInfo
            {
                get { return _customInfo; }
            }


            // * in question template the answer term should be ANS and other vars should be VAR1, VAR2, etc
            // * answer index is the index into the var array that is the var that should be used as the answer
            // * varCalc is an array of expressions that will be used to workout the vars (so var naming is important as each
            // named var will be matched to the value calculated by the corresponding evaluation expression
            // NOTE: the answer var is specified as indexed rather than, say, having it always as the first
            // or last var, because depending on the expression we will need to calculate the answer
            // var before or after other vars have been calculated.  By using an index we sacrifice a bit of
            // readbility in when building the template expressions, but we gain a lot of
            // flexibility to handle more kinds of expressions than we otherwise would be able
            // to express.
            public QuestionAnswerIntegerTemplate(string questionTemplate, int answerIndx, string[] varsCalc, string customInfo)
            {
                _questionTemplate = questionTemplate;
                _answerIndx = answerIndx;
                _varsCalc = varsCalc;
                _customInfo = customInfo;
            }

            public void EvaluateTemplate()
            {
                // process each var calculation expression in turn building up a list of vars
                int count = _varsCalc.Length;
                _vars = new string[count];

                for (int i = 0; i < count; i++)
                {
                    // ...substitute any vars already calculated into the expression and evaluate it to get the next var
                    _vars[i] = ((int)(MathExpression.Parse(DoSubstitution(_varsCalc[i])).Evaluate().Value)).ToString();
                }

                // now we can substitute the answer terms and all vars into the question template
                _question = DoSubstitution(_questionTemplate);
            }

            private string DoSubstitution(string targetString)
            {
                string ret = targetString;

                // substitute in all vars that we have available
                int count = _vars.Length;

                for (int i = 0; i < count; i++)
                {
                    if (_vars[i] == null) break;

                    ret = ret.Replace("VAR" + i, _vars[i]);
                }

                return ret;
            }
        }
    }
}
