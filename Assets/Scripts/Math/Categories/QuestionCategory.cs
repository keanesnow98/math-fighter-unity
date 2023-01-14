using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MathFreak.Math.Questions;
using GarageGames.Torque.Core;
using MathFreak.GamePlay;

namespace MathFreak.Math.Categories
{
    /// <summary>
    /// Base class for all question categories.  Each derived category class will know it's
    /// category name and what types of questions should be in it's levels, etc.  The derived
    /// category classes are basically there just to implement the level creation functionality;
    /// the rest is done in this base class.
    /// </summary>
    public abstract class QuestionCategory
    {
        private string _catName;
        private List<Level> _levels;

        public string CatName
        {
            get { return _catName; }
        }


        /// <summary>
        /// Call this constructor to create the category with ALL it's levels included
        /// </summary>
        public QuestionCategory(string catName)
        {
            _catName = catName;
            _levels = new List<Level>();

            for (int i = 1; i <= GetUniqueLevelCount(); i++)
            {
                CreateAndAddLevel(i);
            }
        }

        /// <summary>
        /// Call this constructor to create the category just the specified level included
        /// </summary>
        public QuestionCategory(string catName, int levelNum)
        {
            _catName = catName;
            _levels = new List<Level>();

            CreateAndAddLevel(levelNum);
        }

        // should return the number of unique types of question that the category holds (maximum of 10)
        protected virtual int GetUniqueLevelCount()
        {
            return 10;
        }

        protected void AddLevel(Level level)
        {
            Assert.Fatal(!_levels.Contains(level), "Trying to add a duplicate level to a category (cat:" + _catName + "/level:" + level.LevelName + ")");

            _levels.Add(level);
        }

        protected void CreateAndAddLevel(int levelNum)
        {
            switch (levelNum)
            {
                case 1:
                    CreateAndAddLevel_1();
                    break;

                case 2:
                    CreateAndAddLevel_2();
                    break;

                case 3:
                    CreateAndAddLevel_3();
                    break;

                case 4:
                    CreateAndAddLevel_4();
                    break;

                case 5:
                    CreateAndAddLevel_5();
                    break;

                case 6:
                    CreateAndAddLevel_6();
                    break;

                case 7:
                    CreateAndAddLevel_7();
                    break;

                case 8:
                    CreateAndAddLevel_8();
                    break;

                case 9:
                    CreateAndAddLevel_9();
                    break;

                case 10:
                    CreateAndAddLevel_10();
                    break;

                default:
                    Assert.Fatal(false, "Level out of range: " + levelNum);
                    CreateAndAddLevel_1();
                    break;
            }
        }

        protected virtual void CreateAndAddLevel_1() {}
        protected virtual void CreateAndAddLevel_2() {}
        protected virtual void CreateAndAddLevel_3() {}
        protected virtual void CreateAndAddLevel_4() {}
        protected virtual void CreateAndAddLevel_5() {}
        protected virtual void CreateAndAddLevel_6() {}
        protected virtual void CreateAndAddLevel_7() {}
        protected virtual void CreateAndAddLevel_8() {}
        protected virtual void CreateAndAddLevel_9() {}
        protected virtual void CreateAndAddLevel_10() {}

        /// <summary>
        /// This should only be called when the category has been created with ALL levels included
        /// </summary>
        public string GetLevelName(int levelNum)
        {
            Assert.Fatal(_levels.Count == GetUniqueLevelCount() && levelNum < _levels.Count, "Level index is out of range: (" + levelNum + " vs " + _levels.Count + ")");

            if (levelNum < _levels.Count)
            {
                return _levels[levelNum].LevelName;
            }
            else
            {
                return "<<empty>>";
            }
        }

        /// <summary>
        /// Call this to get a question from this category
        /// </summary>
        public QuestionContent GetQuestion()
        {
            Assert.Fatal(_levels.Count > 0, "Trying to get a question when there are no levels currently set");

            Level level;

            // if only one level then use that one
            if (_levels.Count == 1)
            {
                level = _levels[0];
            }
            // else pick a level at random
            else
            {
                level = _levels[Game.Instance.Rnd.Next(0, GetUniqueLevelCount())];
            }

            // get a question from the selected category
            string[] content = level.GetQuestion();

            // fill out a QuestionContent data structure to return to the caller
            QuestionContent question = new QuestionContent();
            question.Question = content[0];
            question.Answers = new string[4];
            question.Answers[0] = content[1];
            question.Answers[1] = content[2];
            question.Answers[2] = content[3];
            question.Answers[3] = content[4];

            if (level.CanRandomizeAnswerOrder())
            {
                // at this point the right answer is always the first one, but we will now swap the right
                // answer with another answer at random - obv we record the right answer for later reference
                question.RightAnswer = Game.Instance.Rnd.Next(0, 4);

                if (question.RightAnswer != 0)
                {
                    string temp = question.Answers[0];
                    question.Answers[0] = question.Answers[question.RightAnswer];
                    question.Answers[question.RightAnswer] = temp;
                }
            }
            else
            {
                // the right answer will be specified in an extra string
                question.RightAnswer = int.Parse(content[6]);
            }

            // fill in question/level info
            question.CatName = _catName;
            question.LevelName = level.LevelName;

            // fill in various parameters associated with presenting a question
            question.TimeAllowed = GetTimeAllowed();
            question.Points = GetPoints();
            question.Tutor = GetTutor();
            question.AIMinTime[0] = level.GetMinEasyTime();
            question.AIMinTime[1] = level.GetMinMediumTime();
            question.AIMinTime[2] = level.GetMinHardTime();
            question.AIMinTime[3] = level.GetMinExpertTime();
            question.AIMaxTime[0] = level.GetMaxEasyTime();
            question.AIMaxTime[1] = level.GetMaxMediumTime();
            question.AIMaxTime[2] = level.GetMaxHardTime();
            question.AIMaxTime[3] = level.GetMaxExpertTime();
            question.AIAvgTime[0] = level.GetAvgEasyTime();
            question.AIAvgTime[1] = level.GetAvgMediumTime();
            question.AIAvgTime[2] = level.GetAvgHardTime();
            question.AIAvgTime[3] = level.GetAvgExpertTime();

            //question.Hint = content[5];
            question.Hint = level.GetHint();

            return question;
        }

        protected virtual float GetTimeAllowed()
        {
            return 15.0f;
        }

        protected virtual int GetPoints()
        {
            return 10;
        }

        protected virtual TutorManager.EnumTutor GetTutor()
        {
            // default tutor
            return TutorManager.EnumTutor.Robot;
        }
        
        /// <summary>
        /// This class encapsulates a single level - categories will create instances of this class
        /// and provide it with a question type to use.
        /// </summary>
        protected class Level
        {
            private string _levelName;
            private MathQuestion _mq;

            public string LevelName
            {
                get { return _levelName; }
            }

            public Level(string levelName, MathQuestion mq)
            {
                _levelName = levelName;
                _mq = mq;
            }

            public bool CanRandomizeAnswerOrder()
            {
                return _mq.CanRandomizeAnswerOrder();
            }

            public float GetMinEasyTime()
            {
                return _mq.GetMinEasyTime();
            }

            public float GetMinMediumTime()
            {
                return _mq.GetMinMediumTime();
            }

            public float GetMinHardTime()
            {
                return _mq.GetMinHardTime();
            }

            public float GetMinExpertTime()
            {
                return _mq.GetMinExpertTime();
            }

            public float GetMaxEasyTime()
            {
                return _mq.GetMaxEasyTime();
            }

            public float GetMaxMediumTime()
            {
                return _mq.GetMaxMediumTime();
            }

            public float GetMaxHardTime()
            {
                return _mq.GetMaxHardTime();
            }

            public float GetMaxExpertTime()
            {
                return _mq.GetMaxExpertTime();
            }

            public float GetAvgEasyTime()
            {
                return _mq.GetAvgEasyTime();
            }

            public float GetAvgMediumTime()
            {
                return _mq.GetAvgMediumTime();
            }

            public float GetAvgHardTime()
            {
                return _mq.GetAvgHardTime();
            }

            public float GetAvgExpertTime()
            {
                return _mq.GetAvgExpertTime();
            }

            public float GetTimeAllowed()
            {
                return _mq.GetTimeAllowed();
            }

            public string GetHint()
            {
                return _mq.GetHint();
            }

            /// <summary>
            /// Returns an array of five strings representing the question and answers.
            /// String[0] is the question string and string[1] is the correct answer string.
            /// The remaining three strings are the decoy answers.
            /// </summary>
            public string[] GetQuestion()
            {
                return _mq.GetContent();
            }
        }
    }



    /// <summary>
    /// This structure holds all the info that is needed by the game for presenting
    /// a question to the player.
    /// </summary>
    public class QuestionContent
    {
        public string Question;
        public string[] Answers;
        public int RightAnswer;
        public string CatName;
        public string LevelName;
        public float TimeAllowed;
        public int Points;
        public TutorManager.EnumTutor Tutor;
        public string Hint;

        /* 
         * The following properties are not sent across the network in a LIVE multiplayer game as
         * they are only relevant to the singleplayer game
         */
        public float[] AIMinTime = new float[4];
        public float[] AIMaxTime = new float[4];
        public float[] AIAvgTime = new float[4];


        //public QuestionContent GetCopy()
        //{
        //    QuestionContent content = new QuestionContent();

        //    content.CatName = CatName;
        //    content.LevelName = LevelName;
        //    content.Question = Question;
        //    content.RightAnswer = RightAnswer;

        //    if (Answers != null)
        //    {
        //        content.Answers = new string[4];

        //        for (int i = 0; i < 4; i++)
        //        {
        //            content.Answers[i] = Answers[i];
        //        }
        //    }

        //    content.TimeAllowed = TimeAllowed;
        //    content.Points = Points;
        //    content.Tutor = Tutor;

        //    return content;
        //}

        //public bool Equals(QuestionContent content)
        //{
        //    if (content == null) return false;

        //    if (CatName != content.CatName) return false;
        //    if (LevelName != content.LevelName) return false;
        //    if (Question != content.Question) return false;
        //    if (RightAnswer != content.RightAnswer) return false;

        //    for (int i = 0; i < 4; i++)
        //    {
        //        if (Answers[i] != content.Answers[i]) return false;
        //    }

        //    if (TimeAllowed != content.TimeAllowed) return false;
        //    if (Points != content.Points) return false;
        //    if (Tutor != content.Tutor) return false;

        //    return true;
        //}

        public override string ToString()
        {
            string ret = "";

            if (CatName == null)
            {
                ret += "catname: <none>\n";
            }
            else
            {
                ret += "catname: " + CatName + "\n";
            }

            if (LevelName == null)
            {
                ret += "levelname: <none>\n";
            }
            else
            {
                ret += "levelname: " + LevelName + "\n";
            }

            if (Question == null)
            {
                ret += "question: <none>\n";
            }
            else
            {
                ret += "question: " + Question + "\n";
            }

            if (Answers == null)
            {
                ret += "<no answers>\n";
            }
            else
            {
                for (int i = 0; i < Answers.Length; i++)
                {
                    ret += "Answer[" + i + "] => " + Answers[i] + "\n";
                }
            }

            ret += "right answer: " + RightAnswer + "\n";
            ret += "time allowed: " + TimeAllowed + "\n";
            ret += "points: " + Points + "\n";
            ret += "tutor: " + Tutor + "\n";

            return ret;
        }
    }
}
