using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;



namespace MathFreak.Math.Questions
{
    /// <summary>
    /// Generates advanced subtraction problems if the form: x - y - z
    /// </summary>
    public class DMQ_SimpleSubtraction : DynamicMathQuestion
    {
        private const int MIN_VAR1 = 3; // note: this min is 3 instead of 1 so that we can garauntee there being at least two numbers (both 1) that we can subtract from the first number without getting a negative result
        private const int MAX_VAR1 = 51;
        private const int MIN_VAR2 = 1;
        private const int MIN_VAR3 = 1;


        public override string[] GetContent()
        {
            string[] content = new string[6];

            // create the vars
            int var1 = _rnd.Next(MIN_VAR1, MAX_VAR1);
            int var2 = _rnd.Next(MIN_VAR2, var1 - 1);
            int var3 = _rnd.Next(MIN_VAR3, var1 - var2);
            int varResult = var1 - var2 - var3;
            
            // create the question
            content[0] = var1.ToString() + " - " + var2.ToString() + " - " + var3.ToString() + " = ?";

            // create the correct answer
            content[1] = varResult.ToString();

            // create the decoy answers (perturb the correct answer by some amount in the range -5 to +5, but not including zero obviously)

            // create the pertubations - should not perturb by zero and should all be different
            Dictionary<int, int> alreadyUsed = new Dictionary<int, int>();

            for (int i = 2; i < 5; i++)
            {
                int pertubation;

                do
                {
                    pertubation = _rnd.Next(-5, 5);

                } while (pertubation == 0 || alreadyUsed.ContainsKey(pertubation) || varResult + pertubation < 1);

                content[i] = (varResult + pertubation).ToString();
                alreadyUsed.Add(pertubation, i);
            }

            // provide a hint text
            content[5] = "This is the hint for advanced subtraction";

            return content;
        }
    }
}
