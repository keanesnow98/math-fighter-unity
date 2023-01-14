using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GarageGames.Torque.Core;



namespace MathFreak.Math.Questions
{
    /// <summary>
    /// Player must workout what the PEMDAS expression evaluates to (a PEMDAS expression is an
    /// expression using the +-/*() operators).
    /// </summary>
    public class DMQ_PEMDAS : DynamicMathQuestion
    {
        // hand coded templates for the pemdas expressions so that they are well controlled
        // with regard to the demands being placed on the player.
        private QuestionAnswerIntegerTemplate[] _qaTemplates = new QuestionAnswerIntegerTemplate[]
            {
                // a + b * c + d
                new QuestionAnswerIntegerTemplate(
                    "VAR0 + VAR1 * VAR2 + VAR3",
                    4,
                    new string[]
                    {
                        "1 RND 20", // a
                        "1 RND 12", // b
                        "1 RND 12", // c
                        "1 RND 20", // d
                        "VAR0 + VAR1 * VAR2 + VAR3", // answer
                    },
                    "1.0"),

                // a - b * c + d
                new QuestionAnswerIntegerTemplate(
                    "VAR0 - VAR1 * VAR2 + VAR3",
                    4,
                    new string[]
                    {
                        "1 RND 20", // a
                        "1 RND 12", // b
                        "1 RND 12", // c
                        "1 RND 20", // d
                        "VAR0 - VAR1 * VAR2 + VAR3", // answer
                    },
                    "1.0"),

                // a - b * c - d
                new QuestionAnswerIntegerTemplate(
                    "VAR0 - VAR1 * VAR2 - VAR3",
                    4,
                    new string[]
                    {
                        "1 RND 20", // a
                        "1 RND 12", // b
                        "1 RND 12", // c
                        "1 RND 20", // d
                        "VAR0 - VAR1 * VAR2 - VAR3", // answer
                    },
                    "1.0"),

                // a + b * c - d
                new QuestionAnswerIntegerTemplate(
                    "VAR0 + VAR1 * VAR2 - VAR3",
                    4,
                    new string[]
                    {
                        "1 RND 20", // a
                        "1 RND 12", // b
                        "1 RND 12", // c
                        "1 RND 20", // d
                        "VAR0 + VAR1 * VAR2 - VAR3", // answer
                    },
                    "1.0"),

                // a + b * (c - d)
                new QuestionAnswerIntegerTemplate(
                    "VAR0 + VAR1 * ( VAR2 - VAR3 )",
                    4,
                    new string[]
                    {
                        "1 RND 20", // a
                        "1 RND 12", // a
                        "1 RND 6", // c
                        "1 RND 6", // d
                        "VAR0 + VAR1 * ( VAR2 - VAR3 )", // answer
                    },
                    "1.0"),

                // a - b * (c + d)
                new QuestionAnswerIntegerTemplate(
                    "VAR0 - VAR1 * ( VAR2 + VAR3 )",
                    4,
                    new string[]
                    {
                        "1 RND 20", // a
                        "1 RND 12", // a
                        "1 RND 6", // c
                        "1 RND 6", // d
                        "VAR0 - VAR1 * ( VAR2 + VAR3 )", // answer
                    },
                    "1.0"),

                // a - b * (c + d)
                new QuestionAnswerIntegerTemplate(
                    "VAR0 - VAR1 * ( VAR2 + VAR3 )",
                    4,
                    new string[]
                    {
                        "1 RND 20", // a
                        "1 RND 12", // a
                        "1 RND 6", // c
                        "1 RND 6", // d
                        "VAR0 - VAR1 * ( VAR2 + VAR3 )", // answer
                    },
                    "1.0"),
            };


        public override string[] GetContent()
        {
            string[] content = new string[6];

            // choose and evaluate a template
            QuestionAnswerIntegerTemplate template = _qaTemplates[_rnd.Next(0, _qaTemplates.Length)];
            template.EvaluateTemplate();

            // create the question
            content[0] = "Solve#@height{" + template.CustomInfo + "}#" + WrapMathString(template.Question) + "#= ?";

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
            content[5] = "This is the hint for PEMDAS";

            return content;
        }
    }
}
