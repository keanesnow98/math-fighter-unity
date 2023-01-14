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
    public class QuestionCategory_4 : QuestionCategory
    {
        private const string CATNAME = "Grade 4";

        public QuestionCategory_4()
            : base(CATNAME)
        {
            // do nothing - just here to pass on the category name
        }

        public QuestionCategory_4(int levelNum)
            : base(CATNAME, levelNum)
        {
            // do nothing - just here to pass on the category name
        }

        protected override int GetUniqueLevelCount()
        {
            return 6;
        }

        protected override void CreateAndAddLevel_1()
        {
            AddLevel(new Level("Money", new DMQ_Money()));
        }

        protected override void CreateAndAddLevel_2()
        {
            AddLevel(new Level("Advanced Division", new DMQ_SimpleDivision()));
        }

        protected override void CreateAndAddLevel_3()
        {
            AddLevel(new Level("Rounding Numbers", new DMQ_RoundingNumbers()));
        }

        protected override void CreateAndAddLevel_4()
        {
            AddLevel(new Level("Decimal to Percent", new DMQ_DecimalToPercentage()));
        }

        protected override void CreateAndAddLevel_5()
        {
            AddLevel(new Level("Advanced Multiplication", new DMQ_SimpleMultiplication()));
        }

        protected override void CreateAndAddLevel_6()
        {
            AddLevel(new Level("Store Sale", new DMQ_StoreSale()));
        }
    }
}
