using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MathFreak.Math.Questions;
using GarageGames.Torque.Core;

namespace MathFreak.Math.Categories
{
    /// <summary>
    /// The implementation for question category 0.
    /// </summary>
    public class QuestionCategory_0 : QuestionCategory
    {
        private const string CATNAME = "Easy Math";

        public QuestionCategory_0()
            : base(CATNAME)
        {
            // do nothing - just here to pass on the category name
        }

        public QuestionCategory_0(int levelNum)
            : base(CATNAME, levelNum)
        {
            // do nothing - just here to pass on the category name
        }

        protected override void CreateAndAddLevel_1()
        {
            AddLevel(new Level("Level 1", new DMQ_SimpleAdditionNoNeg()));
        }

        protected override void CreateAndAddLevel_2()
        {
            AddLevel(new Level("Level 2", new DMQ_SimpleSubtractionNoNeg()));
        }

        protected override void CreateAndAddLevel_3()
        {
            AddLevel(new Level("Level 3", new DMQ_SimpleMultiplicationNoNeg()));
        }

        protected override void CreateAndAddLevel_4()
        {
            AddLevel(new Level("Level 4", new DMQ_SimpleAdditionNoNeg()));
        }

        protected override void CreateAndAddLevel_5()
        {
            AddLevel(new Level("Level 5", new DMQ_SimpleAdditionNoNeg()));
        }

        protected override void CreateAndAddLevel_6()
        {
            AddLevel(new Level("Level 6", new DMQ_SimpleSubtractionNoNeg()));
        }

        protected override void CreateAndAddLevel_7()
        {
            AddLevel(new Level("Level 7", new DMQ_SimpleMultiplicationNoNeg()));
        }

        protected override void CreateAndAddLevel_8()
        {
            AddLevel(new Level("Level 8", new DMQ_SimpleMultiplicationNoNeg()));
        }

        protected override void CreateAndAddLevel_9()
        {
            AddLevel(new Level("Level 9", new DMQ_SimpleMultiplicationNoNeg()));
        }

        protected override void CreateAndAddLevel_10()
        {
            AddLevel(new Level("Level 10", new DMQ_SimpleSubtractionNoNeg()));
        }
    }
}
