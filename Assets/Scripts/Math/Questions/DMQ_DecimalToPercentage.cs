using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;



namespace MathFreak.Math.Questions
{
    /// <summary>
    /// Generates problems where decimals must be converted to percentages
    /// </summary>
    public class DMQ_DecimalToPercentage : DynamicMathQuestion
    {
        public override string[] GetContent()
        {
            string[] content = new string[6];

            // create the vars
            float var1 = (float)_rnd.NextDouble();

            if (var1 < 0.01f) var1 = 0.01f;
            
            float varResult = var1 * 100.0f;
            
            // create the question
            content[0] = "Convert " + GetStringWithSignificantFigures(var1, 3) + " to a percentage";

            // create the correct answer
            string answer = GetStringWithSignificantFigures(varResult, 1);
            content[1] = answer + "%";

            // create the decoy answers
            content[2] = GetStringWithSignificantFigures(float.Parse(answer) * 10.0f, 1) + "%";
            content[3] = (System.Math.Round(float.Parse(answer) / 10.0f, 2)).ToString() + "%";
            content[4] = (System.Math.Round(float.Parse(answer) / 100.0f, 3)).ToString() + "%";

            // provide a hint text
            content[5] = "This is the hint for simple decimal to percentage";

            return content;
        }
    }
}
