using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;



namespace MathFreak.Math.Questions
{
    /// <summary>
    /// Generates advanced division problems of the form: x / y
    /// </summary>
    public class DMQ_SimpleDivision : DynamicMathQuestion
    {
        private const int MIN_VARRESULT = 1;
        private const int MAX_VARRESULT = 51;
        private const int NOMINATOR_MAX = 100;

        public override string[] GetContent()
        {
            string[] content = new string[6];

            // create the vars - we actually choose the varResult ourselves to make sure it's an integer then workout what var1 needs to be to make the question work
            int varResult = _rnd.Next(MIN_VARRESULT, MAX_VARRESULT);
            int var1 = _rnd.Next(2, (NOMINATOR_MAX / varResult) + 1);   // min denominator is 2
            int var2 = var1 * varResult;

            // create the question
            content[0] = WrapMathString(var2.ToString() + " // " + var1.ToString()) + "#= ?";

            // create the correct answer
            content[1] = varResult.ToString();

            // create the decoy answers (perturb the correct answer by some amount in the range -5 to +5, but not including zero obviously)

            // create the pertubations - should not perturb by zero and should all be different
            Dictionary<int, int> alreadyUsed = new Dictionary<int, int>();
            alreadyUsed.Add(varResult, 1);

            for (int i = 2; i < 5; i++)
            {
                int pertubation;

                do
                {
                    pertubation = _rnd.Next(-5, 5);

                } while (alreadyUsed.ContainsKey(varResult + pertubation) || varResult + pertubation < 1);

                content[i] = (varResult + pertubation).ToString();
                alreadyUsed.Add(varResult + pertubation, i);
            }

            // provide a hint text
            content[5] = "This is the hint for advanced division";

            return content;
        }
    }
}
