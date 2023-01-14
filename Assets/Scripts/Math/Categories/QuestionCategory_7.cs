using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MathFreak.Math.Questions;
using GarageGames.Torque.Core;

namespace MathFreak.Math.Categories
{
    /// <summary>
    /// The implementation for question category 7.
    /// </summary>
    public class QuestionCategory_7 : QuestionCategory
    {
        private const string CATNAME = "Grade 7";

        public QuestionCategory_7()
            : base(CATNAME)
        {
            // do nothing - just here to pass on the category name
        }

        public QuestionCategory_7(int levelNum)
            : base(CATNAME, levelNum)
        {
            // do nothing - just here to pass on the category name
        }

        protected override int GetUniqueLevelCount()
        {
            return 5;
        }

        protected override void CreateAndAddLevel_1()
        {
            AddLevel(new Level("PEMDAS", new DMQ_PEMDAS()));
        }

        protected override void CreateAndAddLevel_2()
        {
            AddLevel(new Level("Division With Negatives", new DMQ_DivisionWithNeg()));
        }

        protected override void CreateAndAddLevel_3()
        {
            AddLevel(new Level("Addition With Negatives", new DMQ_AdditionWithNeg()));
        }

        protected override void CreateAndAddLevel_4()
        {
            AddLevel(new Level("Subtraction With Negatives", new DMQ_SubtractionWithNeg()));
        }

        protected override void CreateAndAddLevel_5()
        {
            AddLevel(new Level("Multiplication With Negatives", new DMQ_MultiplicationWithNeg()));
        }
    }
}
