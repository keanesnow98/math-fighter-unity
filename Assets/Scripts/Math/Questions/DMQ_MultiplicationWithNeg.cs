using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;



namespace MathFreak.Math.Questions
{
    /// <summary>
    /// Generates advanced multiplication problems if the form: x * y
    /// </summary>
    public class DMQ_MultiplicationWithNeg : DynamicMathQuestion
    {
        private const int MIN_VAR1 = -12;
        private const int MAX_VAR1 = 13;
        private const int MIN_VAR2 = -12;
        private const int MAX_VAR2 = 13;

        public override string[] GetContent()
        {
            string[] content = new string[6];

            // create the vars
            int var1 = _rnd.Next(MIN_VAR1, MAX_VAR1);
            int var2 = _rnd.Next(MIN_VAR2, MAX_VAR2);
            int varResult = var1 * var2;
            
            // create the question
            content[0] = WrapMathString(var1.ToString() + " * " + var2.ToString()) + "#= ?";

            // create the correct answer
            content[1] = varResult.ToString();

            // create the decoy answers (perturb the correct answer by some amount in the range -5 to +5, but not including zero obviously)

            // create the pertubations - should not perturb by zero and should all be different
            Dictionary<int, int> alreadyUsed = new Dictionary<int, int>();
            alreadyUsed.Add(varResult, 1);

            // ...special case if answer is zero
            if (varResult == 0)
            {
                int pertresult = varResult + _rnd.Next(1, 5);
                content[2] = pertresult.ToString();
                alreadyUsed.Add(pertresult, 2);
            }
            // ...non-special case - just negate the answer
            else
            {
                content[2] = (-varResult).ToString();
                alreadyUsed.Add(-varResult, 2);
            }

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
            content[5] = "This is the hint for multiplication";

            return content;
        }
    }
}
