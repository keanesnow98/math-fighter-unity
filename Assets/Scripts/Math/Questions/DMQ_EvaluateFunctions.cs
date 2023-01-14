using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GarageGames.Torque.Core;



namespace MathFreak.Math.Questions
{
    /// <summary>
    /// Player must evaluate functions such as: "Evaluate F(x) = 2x + (x - 1), where x = 2"
    /// </summary>
    public class DMQ_EvaluateFunctions : DynamicMathQuestion
    {
        // hand coded templates for the pemdas expressions so that they are well controlled
        // with regard to the demands being placed on the player.
        private QuestionAnswerIntegerTemplate[] _qaTemplates = new QuestionAnswerIntegerTemplate[]
            {
                // a * x + (x - b)
                new QuestionAnswerIntegerTemplate(
                    "VAR0 * x + ( x - VAR1 ), where x = VAR2",
                    3,
                    new string[]
                    {
                        "2 RND 12", // a
                        "1 RND 20", // b
                        "1 RND 12", // x
                        "VAR0 * VAR2 + ( VAR2 - VAR1 )", // answer
                    },
                    "1.0"),

                // a * x - (x + b)
                new QuestionAnswerIntegerTemplate(
                    "VAR0 * x - ( x + VAR1 ), where x = VAR2",
                    3,
                    new string[]
                    {
                        "2 RND 12", // a
                        "1 RND 20", // b
                        "1 RND 12", // x
                        "VAR0 * VAR2 - ( VAR2 + VAR1 )", // answer
                    },
                    "1.0"),

                // (ax + b) ^ 2
                new QuestionAnswerIntegerTemplate(
                    "( VAR0 * x + VAR2 ) ^ 2, where x = VAR1",
                    3,
                    new string[]
                    {
                        "2 RND 3", // a
                        "1 RND 3", // x
                        "1 RND 3", // b
                        "( VAR0 * VAR1 + VAR2 ) ^ 2", // answer
                    },
                    "1.0"),

                // (ax - b) ^ 2
                new QuestionAnswerIntegerTemplate(
                    "( VAR0 * x - VAR2 ) ^ 2, where x = VAR1",
                    3,
                    new string[]
                    {
                        "1 RND 3", // a
                        "1 RND 3", // x
                        "1 RND 10", // b
                        "( VAR0 * VAR1 - VAR2 ) ^ 2", // answer
                    },
                    "1.0"),

                // (ax - b) ^ 2
                new QuestionAnswerIntegerTemplate(
                    "( VAR0 * x - VAR2 ) ^ 2, where x = VAR1",
                    3,
                    new string[]
                    {
                        "2", // a
                        "2", // x
                        "9", // b
                        "( VAR0 * VAR1 - VAR2 ) ^ 2", // answer
                    },
                    "1.0"),
            };


        public override string[] GetContent()
        {
            string[] content = new string[6];

            // choose and evaluate a template
            QuestionAnswerIntegerTemplate template = _qaTemplates[4];
            //QuestionAnswerIntegerTemplate template = _qaTemplates[_rnd.Next(0, _qaTemplates.Length)];
            template.EvaluateTemplate();

            // create the question
            string[] questionParts = template.Question.Split(',');
            content[0] = "Evaluate#@height{" + template.CustomInfo + "}#F(x) =#" + WrapMathString(questionParts[0]) + "#," + questionParts[1];

            // fill in the answer
            content[1] = template.Answer;

            // create the decoy answers (perturb the answer by some amount)

            // ...create the pertubations - should all be different
            Dictionary<string, int> alreadyUsed = new Dictionary<string, int>();
            alreadyUsed.Add(content[1], 0);

            int answerValue = int.Parse(template.Answer);

            for (int i = 2; i < 5; i++)
            {
                string pertAnswer;

                do
                {
                    pertAnswer = (answerValue + _rnd.Next(-10, 11)).ToString();

                } while (alreadyUsed.ContainsKey(pertAnswer));

                content[i] = pertAnswer;
                alreadyUsed.Add(pertAnswer, i);
            }

            // provide a hint text
            content[5] = "This is the hint for Evaluating Functions";

            return content;
        }
    }
}
