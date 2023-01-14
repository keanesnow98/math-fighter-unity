using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MathFreak.Math.Questions;
using GarageGames.Torque.Core;

namespace MathFreak.Math.Categories
{
    /// <summary>
    /// The implementation for question category 1.
    /// </summary>
    public class QuestionCategory_3 : QuestionCategory
    {
        private const string CATNAME = "Grade 3";

        public QuestionCategory_3()
            : base(CATNAME)
        {
            // do nothing - just here to pass on the category name
        }

        public QuestionCategory_3(int levelNum)
            : base(CATNAME, levelNum)
        {
            // do nothing - just here to pass on the category name
        }

        protected override int GetUniqueLevelCount()
        {
            return 4;
        }

        protected override void CreateAndAddLevel_1()
        {
            AddLevel(new Level("Advanced Addition", new DMQ_SimpleAddition()));
        }

        protected override void CreateAndAddLevel_2()
        {
            AddLevel(new Level("Advanced Subtraction", new DMQ_SimpleSubtraction()));
        }

        protected override void CreateAndAddLevel_3()
        {
            AddLevel(new Level("Intermediate Multiplication", new DMQ_SimpleMultiplicationIntermediate()));
        }

        protected override void CreateAndAddLevel_4()
        {
            AddLevel(new Level("Intermediate Division", new DMQ_SimpleDivisionIntermediate()));
        }
    }
}
