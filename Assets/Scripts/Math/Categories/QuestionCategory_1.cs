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
    public class QuestionCategory_1 : QuestionCategory
    {
        private const string CATNAME = "Grade 1";

        public QuestionCategory_1()
            : base(CATNAME)
        {
            // do nothing - just here to pass on the category name
        }

        public QuestionCategory_1(int levelNum)
            : base(CATNAME, levelNum)
        {
            // do nothing - just here to pass on the category name
        }

        protected override int GetUniqueLevelCount()
        {
#if DEBUG
            // special case in debug - the xml file can specify a test question and category 1 will then consist of ONLY that question so we can test it thouroughly
            if (MathQuestion.HasTestQuestion()) return 1;
#endif

            return 5;
        }

        protected override void CreateAndAddLevel_1()
        {
#if DEBUG
            // special case in debug - the xml file can specify a test question and category 1 will then consist of ONLY that question so we can test it thouroughly
            if (MathQuestion.HasTestQuestion())
            {
                AddLevel(new Level("Test Question", MathQuestion.GetTestQuestion()));
                return;
            }
#endif

            AddLevel(new Level("Pattern Recognition", new DMQ_PatternRecognition()));
        }

        protected override void CreateAndAddLevel_2()
        {
            AddLevel(new Level("Greater-than", new DMQ_GreaterThan()));
        }

        protected override void CreateAndAddLevel_3()
        {
            AddLevel(new Level("Less-than", new DMQ_LessThan()));
        }

        protected override void CreateAndAddLevel_4()
        {
            AddLevel(new Level("Basic Addition", new DMQ_SimpleAdditionNoNeg()));
        }

        protected override void CreateAndAddLevel_5()
        {
            AddLevel(new Level("Basic Subtraction", new DMQ_SimpleSubtractionNoNeg()));
        }
    }
}
