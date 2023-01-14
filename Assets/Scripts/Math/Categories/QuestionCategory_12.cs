using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MathFreak.Math.Questions;
using GarageGames.Torque.Core;

namespace MathFreak.Math.Categories
{
    /// <summary>
    /// The implementation for question category 12.
    /// </summary>
    public class QuestionCategory_12 : QuestionCategory
    {
        private const string CATNAME = "Grade 12";

        public QuestionCategory_12()
            : base(CATNAME)
        {
            // do nothing - just here to pass on the category name
        }

        public QuestionCategory_12(int levelNum)
            : base(CATNAME, levelNum)
        {
            // do nothing - just here to pass on the category name
        }

        protected override int GetUniqueLevelCount()
        {
            return 2;
        }

        protected override void CreateAndAddLevel_1()
        {
            AddLevel(new Level("Area", new DMQ_RectangleArea()));
        }

        protected override void CreateAndAddLevel_2()
        {
            AddLevel(new Level("Perimeter", new DMQ_RectanglePerimeter()));
        }

        //protected override void CreateAndAddLevel_3()
        //{
        //    AddLevel(new Level("Slope of a Line", new DMQ_FindSlope()));
        //}
    }
}
