using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;



namespace MathFreak.Math.Questions
{
    /// <summary>
    /// Generates advanced division problems of the form: x / y
    /// </summary>
    public class DMQ_DivisionWithNeg : DynamicMathQuestion
    {
        private const int MIN_VARRESULT = 1;
        private const int MAX_VARRESULT = 51;
        private const int NOMINATOR_MAX = 100;

        public override string[] GetContent()
        {
            string[] content = new string[6];

            // create the vars - we actually choose the varResult ourselves to make sure it's an integer then workout what var1 needs to be to make the question work
            int varResult = _rnd.Next(MIN_VARRESULT, MAX_VARRESULT);
            int var1 = _rnd.Next(1, (NOMINATOR_MAX / varResult) + 1);
            int var2 = var1 * varResult;

            int posNegDecision = _rnd.Next(0, 4);   // four possible pos/neg combos

            switch (posNegDecision)
            {
                // var1 and var2 both positive
                case 0:
                    // do nothing
                    break;

                // var1 negative and var2 positive
                case 1:
                    var1 = -var1;
                    varResult = -varResult;
                    break;

                // var1 positive and var2 negative
                case 2:
                    var2 = -var2;
                    varResult = -varResult;
                    break;

                // var1 and var2 both negative
                case 3:
                    var1 = -var1;
                    var2 = -var2;
                    break;
            }

            // create the question
            content[0] = WrapMathString(var2.ToString() + " / " + var1.ToString()) + "#= ?";

            // create the correct answer
            content[1] = varResult.ToString();

            // create the decoy answers (perturb the correct answer by some amount in the range -5 to +5, but not including zero obviously)

            // create the pertubations - should not perturb by zero and should all be different
            Dictionary<int, int> alreadyUsed = new Dictionary<int, int>();
            alreadyUsed.Add(varResult, 1);

            content[2] = (-varResult).ToString();
            alreadyUsed.Add(-varResult, 2);

            for (int i = 3; i < 5; i++)
            {
                int pertubation;
                int pertResult;

                do
                {
                    pertubation = _rnd.Next(-5, 5);
                    pertResult = varResult + pertubation;

                    if (_rnd.Next(2) == 0)
                    {
                        pertResult = -pertResult;
                    }

                } while (alreadyUsed.ContainsKey(pertResult));

                content[i] = pertResult.ToString();
                alreadyUsed.Add(pertResult, i);
            }

            // provide a hint text
            content[5] = "This is the hint for division";

            return content;
        }
    }
}
