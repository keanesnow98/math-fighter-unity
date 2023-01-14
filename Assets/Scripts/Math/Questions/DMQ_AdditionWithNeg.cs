using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;



namespace MathFreak.Math.Questions
{
    /// <summary>
    /// Generates advanced addition problems if the form: x + y + z
    /// </summary>
    public class DMQ_AdditionWithNeg : DynamicMathQuestion
    {
        private const int MIN_VAR1 = -100;
        private const int MAX_VAR1 = 101;
        private const int MIN_VAR2 = -100;
        private const int MAX_VAR2 = 101;

        public override string[] GetContent()
        {
            string[] content = new string[6];

            // create the vars
            int var1 = _rnd.Next(MIN_VAR1, MAX_VAR1);
            int var2 = _rnd.Next(MIN_VAR2, MAX_VAR2);
            int varResult = var1 + var2;
            
            // create the question
            content[0] = var1.ToString() + " + " + var2.ToString() + " = ?";

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

                } while (alreadyUsed.ContainsKey(varResult + pertubation));

                content[i] = (varResult + pertubation).ToString();
                alreadyUsed.Add(varResult + pertubation, i);
            }

            // provide a hint text
            content[5] = "This is the hint for addition";

            return content;
        }
    }
}
