using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;



namespace MathFreak.Math.Questions
{
    /// <summary>
    /// Generates problems where fractions must be converted to decimals
    /// </summary>
    public class DMQ_FractionToDecimal : DynamicMathQuestion
    {
        private const int MIN_VAR1 = -25;
        private const int MAX_VAR1 = 25;
        private const int MIN_VAR2 = -25;
        private const int MAX_VAR2 = 25;

        public override string[] GetContent()
        {
            string[] content = new string[6];

            // create the vars
            float var1 = (float)_rnd.Next(MIN_VAR1, MAX_VAR1);
            if (var1 == 0.0f) var1 = 1.0f;  // prevent infinity
            float var2 = (float)_rnd.Next(MIN_VAR2, MAX_VAR2);
            if (var2 == 0.0f) var2 = 1.0f;  // prevent divide by zero
            float varResult = var1 / var2;
            
            // create the question
            content[0] = "Convert #" + WrapMathString(var1.ToString() + " / " + var2.ToString(), 0.8f) + "# to Decimal";

            // create the correct answer
            content[1] = GetStringWithSignificantFigures(varResult, 2);

            // create the decoy answers (perturb the correct answer by some amount in the range -5.0f to +5.0f, but not including zero obviously)

            // create the pertubations - should not perturb by zero and should all be different
            Dictionary<string, int> alreadyUsed = new Dictionary<string, int>();

            for (int i = 2; i < 5; i++)
            {
                float pertubation;
                string rep;

                do
                {
                    pertubation = ((float)_rnd.NextDouble() * 10.0f) - 5.0f;
                    rep = GetStringWithSignificantFigures(varResult + pertubation, 2);

                } while (pertubation == 0.0f || alreadyUsed.ContainsKey(rep));

                content[i] = rep;
                alreadyUsed.Add(rep, i);
            }

            // provide a hint text
            content[5] = "This is the hint for fractions to decimal";

            return content;
        }
    }
}
