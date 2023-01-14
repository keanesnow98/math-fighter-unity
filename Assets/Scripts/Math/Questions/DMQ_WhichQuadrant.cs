using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;



namespace MathFreak.Math.Questions
{
    /// <summary>
    /// Player must workout which quadrant the specified coordinate is in
    /// </summary>
    public class DMQ_WhichQuadrant : DynamicMathQuestion
    {
        private const int MIN_VAR1 = -100;
        private const int MAX_VAR1 = 100;
        private const int MIN_VAR2 = -100;
        private const int MAX_VAR2 = 100;


        public override bool CanRandomizeAnswerOrder()
        {
            return false;
        }

        public override string[] GetContent()
        {
            string[] content = new string[7];

            // create the vars
            int var1 = GetNonZeroRandomNumber(MIN_VAR1, MAX_VAR1 + 1, 50);
            int var2 = GetNonZeroRandomNumber(MIN_VAR2, MAX_VAR2 + 1, 50);

            // create the question
            content[0] = "Which quadrant is the point (" + var1 + "," + var2 + ") in ?";

            // record the position of the right answer in the list of possible answers
            int answerPos = 0;

            if (var1 < 0 && var2 > 0)
            {
                answerPos = 0;
            }
            else if (var1 > 0 && var2 > 0)
            {
                answerPos = 1;
            }
            else if (var1 < 0 && var2 < 0)
            {
                answerPos = 2;
            }
            else //if (var1 > 0 && var2 < 0)
            {
                answerPos = 3;
            }

            // fill in the answer list
            content[1] = "Top Left";
            content[2] = "Top Right";
            content[3] = "Bottom Left";
            content[4] = "Bottom Right";

            // provide a hint text
            content[5] = "This is the hint for Quadrants";

            // specify the answer position explicitly
            content[6] = answerPos.ToString();

            return content;
        }
    }
}
