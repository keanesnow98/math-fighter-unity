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
    public class QuestionCategory_2 : QuestionCategory
    {
        private const string CATNAME = "Grade 2";

        public QuestionCategory_2()
            : base(CATNAME)
        {
            // do nothing - just here to pass on the category name
        }

        public QuestionCategory_2(int levelNum)
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
            AddLevel(new Level("Intermediate Addition", new DMQ_SimpleAdditionIntermediate()));
        }

        protected override void CreateAndAddLevel_2()
        {
            AddLevel(new Level("Intermediate Subtraction", new DMQ_SimpleSubtractionIntermediate()));
        }

        protected override void CreateAndAddLevel_3()
        {
            AddLevel(new Level("Basic Multiplication", new DMQ_SimpleMultiplicationNoNeg()));
        }

        protected override void CreateAndAddLevel_4()
        {
            AddLevel(new Level("Basic Division", new DMQ_SimpleDivisionNoNeg()));
        }
    }
}
