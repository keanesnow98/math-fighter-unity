using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using Microsoft.Xna.Framework.Storage;
using System.IO;
using System.Xml.Serialization;
using GarageGames.Torque.Core;
using System.Diagnostics;



namespace MathFreak.Math.Questions
{
    /// <summary>
    /// This is the base class for math questions.  A math question encompasses both the
    /// question statement itself and the multiple-choice answers to the question.
    /// </summary>
    public abstract class MathQuestion
    {
        public const string BLANK_ANSWER = " ";


        /// <summary>
        /// Called to get the content - an array of 5 strings - the first string is the question string,
        /// the second is the correct answer, and the rest are decoy answers.
        /// </summary>
        public abstract string[] GetContent();

        // default is that the game is allowed to randomize the order of the answers - but
        // any given question type can override this if it needs to (e.g. the quadrant question)
        // If a question type returns false for this then it will also need to return an extra
        // string containing the index of the correct answer, as the correct answer cannot always
        // be the zeroth one in this case.
        public virtual bool CanRandomizeAnswerOrder()
        {
            return true;
        }

        /// <summary>
        /// Called to wrap a string in a @math{} string expression for processing by MathMultiTextComponent (or anything else that can process it)
        /// </summary>
        protected string WrapMathString(string expression)
        {
            return "@math{" + expression + "}";
        }

        /// <summary>
        /// As for the other version of this method, but also includes a character height modifier param.
        /// </summary>
        protected string WrapMathString(string expression, float charHeightModifier)
        {
            return "@math{{" + charHeightModifier + "}" + expression + "}";
        }

        //public virtual float GetMinEasyTime()
        //{
        //    return GetTimeAllowed() * 0.4f;
        //}

        //public virtual float GetMinMediumTime()
        //{
        //    return GetTimeAllowed() * 0.3f;
        //}

        //public virtual float GetMinHardTime()
        //{
        //    return GetTimeAllowed() * 0.2f;
        //}

        //public virtual float GetMinExpertTime()
        //{
        //    return GetTimeAllowed() * 0.1f;
        //}

        //public virtual float GetMaxEasyTime()
        //{
        //    return GetTimeAllowed();
        //}

        //public virtual float GetMaxMediumTime()
        //{
        //    return GetTimeAllowed();
        //}

        //public virtual float GetMaxHardTime()
        //{
        //    return GetTimeAllowed();
        //}

        //public virtual float GetMaxExpertTime()
        //{
        //    return GetTimeAllowed();
        //}

        //public virtual float GetAvgEasyTime()
        //{
        //    return GetTimeAllowed() * 0.8f;
        //}

        //public virtual float GetAvgMediumTime()
        //{
        //    return GetTimeAllowed() * 0.6f;
        //}

        //public virtual float GetAvgHardTime()
        //{
        //    return GetTimeAllowed() * 0.4f;
        //}

        //public virtual float GetAvgExpertTime()
        //{
        //    return GetTimeAllowed() * 0.2f;
        //}

        //public virtual float GetTimeAllowed()
        //{
        //    return 15.0f;
        //}

        //public virtual string GetHint()
        //{
        //    return "This is the hint text for " + GetType().Name;
        //}

        public virtual float GetMinEasyTime()
        {
            return _questionParams[GetType()].MinEasyTime;
        }

        public virtual float GetMinMediumTime()
        {
            return _questionParams[GetType()].MinMediumTime;
        }

        public virtual float GetMinHardTime()
        {
            return _questionParams[GetType()].MinHardTime;
        }

        public virtual float GetMinExpertTime()
        {
            return _questionParams[GetType()].MinExpertTime;
        }

        public virtual float GetMaxEasyTime()
        {
            return _questionParams[GetType()].MaxEasyTime;
        }

        public virtual float GetMaxMediumTime()
        {
            return _questionParams[GetType()].MaxMediumTime;
        }

        public virtual float GetMaxHardTime()
        {
            return _questionParams[GetType()].MaxHardTime;
        }

        public virtual float GetMaxExpertTime()
        {
            return _questionParams[GetType()].MaxExpertTime;
        }

        public virtual float GetAvgEasyTime()
        {
            return _questionParams[GetType()].AvgEasyTime;
        }

        public virtual float GetAvgMediumTime()
        {
            return _questionParams[GetType()].AvgMediumTime;
        }

        public virtual float GetAvgHardTime()
        {
            return _questionParams[GetType()].AvgHardTime;
        }

        public virtual float GetAvgExpertTime()
        {
            return _questionParams[GetType()].AvgExpertTime;
        }

        public virtual float GetTimeAllowed()
        {
            return _questionParams[GetType()].TimeAllowed;
        }

        public virtual string GetHint()
        {
            return _questionParams[GetType()].Hint.Trim();
        }

        public static MathQuestion GetTestQuestion()
        {
            if (_testQuestion != null)
            {
                return Activator.CreateInstance(_testQuestion) as MathQuestion;
            }
            else
            {
                return null;
            }
        }

        public static bool HasTestQuestion()
        {
            return _testQuestion != null;
        }


        #region XML data load/save

        private const int VERSION = 1000;

        private static Type[] _questionClasses = new Type[]
            {
                typeof(DMQ_AbsoluteValue),
                typeof(DMQ_AbsoluteValuesAddition),
                typeof(DMQ_AbsoluteValuesMultiplication),
                typeof(DMQ_AdditionWithNeg),
                typeof(DMQ_AlgebraBasicFactoring),
                typeof(DMQ_AlgebraCombineLikeTerms),
                typeof(DMQ_AlgebraDitributiveLaw),
                typeof(DMQ_AlgebraSolveEquations),
                //typeof(DMQ_BoxVolume),
                //typeof(DMQ_CircleArea),
                typeof(DMQ_DecimalToPercentage),
                typeof(DMQ_Derivatives),
                typeof(DMQ_DivisionWithNeg),
                typeof(DMQ_Domains),
                typeof(DMQ_EvaluateFunctions),
                typeof(DMQ_ExponentsPower2),
                typeof(DMQ_ExponentsPower3),
                typeof(DMQ_ExponentsPower4),
                typeof(DMQ_FractionsAddition),
                typeof(DMQ_FractionsCommonDenominator),
                typeof(DMQ_FractionsDivision),
                typeof(DMQ_FractionsImproper),
                typeof(DMQ_FractionsMultiplication),
                typeof(DMQ_FractionsSimplify),
                typeof(DMQ_FractionsSubtraction),
                //typeof(DMQ_FractionToDecimal),
                typeof(DMQ_GreaterThan),
                typeof(DMQ_LessThan),
                typeof(DMQ_Limits),
                //typeof(DMQ_MixedNumberToFraction),
                typeof(DMQ_Money),
                typeof(DMQ_MultiplicationWithNeg),
                typeof(DMQ_PatternRecognition),
                typeof(DMQ_PEMDAS),
                typeof(DMQ_RectangleArea),
                typeof(DMQ_RectanglePerimeter),
                typeof(DMQ_Root2),
                typeof(DMQ_Root3),
                typeof(DMQ_Root4),
                typeof(DMQ_RoundingNumbers),
                //typeof(DMQ_ScientificNotation),
                typeof(DMQ_SimpleAddition),
                typeof(DMQ_SimpleAdditionIntermediate),
                typeof(DMQ_SimpleAdditionNoNeg),
                typeof(DMQ_SimpleDivision),
                typeof(DMQ_SimpleDivisionIntermediate),
                typeof(DMQ_SimpleDivisionNoNeg),
                typeof(DMQ_SimpleMultiplication),
                typeof(DMQ_SimpleMultiplicationIntermediate),
                typeof(DMQ_SimpleMultiplicationNoNeg),
                typeof(DMQ_SimpleSubtraction),
                typeof(DMQ_SimpleSubtractionIntermediate),
                typeof(DMQ_SimpleSubtractionNoNeg),
                typeof(DMQ_StoreSale),
                typeof(DMQ_SubtractionWithNeg),
                //typeof(DMQ_TriangleArea),
                typeof(DMQ_TruncatingNumbers),
                typeof(DMQ_WhichQuadrant),
            };

        protected static Dictionary<Type, QuestionParams> _questionParams = new Dictionary<Type, QuestionParams>(60);
        protected static Type _testQuestion;

        /// <summary>
        /// Called to load all the tweakable data that is stored in xml: question to use for testing,
        /// question time limits, AI variables, hint texts.
        /// </summary>
        public static void LoadData()
        {
            FileStream file = null;

            try
            {
                // Add the container path to our file name.
                String filename = Path.Combine(StorageContainer.TitleLocation, "data/xml/math.dat");

                file = File.Open(filename, FileMode.Open, FileAccess.Read);

                // deserialize
                QuestionXMLData data = new QuestionXMLData();
                XmlSerializer serializer = new XmlSerializer(typeof(QuestionXMLData));
                data = (QuestionXMLData)serializer.Deserialize(file);

                if (data.Version != VERSION)
                {
                    throw new InvalidDataException("math.dat is wrong version number: " + data.Version + " vs " + VERSION);
                }

                // fill the database
                foreach (QuestionParams qp in data.Questions)
                {
                    _questionParams.Add(Type.GetType("MathFreak.Math.Questions." + qp.InternalName), qp);
                }

                // set the test question to use
                _testQuestion = Type.GetType("MathFreak.Math.Questions." + data.TestQuestion);
            }
            catch (Exception e)
            {
                Assert.Fatal(false, "Error: " + e.Message);
                Debug.WriteLine("Error: " + e.Message);
                Game.Instance.Exit();
            }
            finally
            {
                if (file != null)
                {
                    file.Close();
                }
            }
        }

#if DEBUG
        // This is here to auto generate the initial xml file that can then be edited.
        public static void SaveData()
        {
            // Add the container path to our file name.
            String filename = Path.Combine(StorageContainer.TitleLocation, "data/xml/math.dat");
            //FileStream file = File.Open("data/xml/math.dat", FileMode.Create);
            FileStream file = File.Open(filename, FileMode.Create);

            QuestionXMLData data = new QuestionXMLData();

            data.TestQuestion = "none";

            data.Questions = new QuestionParams[_questionClasses.Length];

            for (int i = 0; i < _questionClasses.Length; i++)
            {
                Type typeinfo = _questionClasses[i];
                MathQuestion question = Activator.CreateInstance(typeinfo) as MathQuestion;
                QuestionParams qp = new QuestionParams();

                qp.InternalName = typeinfo.Name;
                qp.TimeAllowed = question.GetTimeAllowed();
                qp.MinEasyTime = question.GetMinEasyTime();
                qp.AvgEasyTime = question.GetAvgEasyTime();
                qp.MaxEasyTime = question.GetMaxEasyTime();
                qp.MinMediumTime = question.GetMinMediumTime();
                qp.AvgMediumTime = question.GetAvgMediumTime();
                qp.MaxMediumTime = question.GetMaxMediumTime();
                qp.MinHardTime = question.GetMinHardTime();
                qp.AvgHardTime = question.GetAvgHardTime();
                qp.MaxHardTime = question.GetMaxHardTime();
                qp.MinExpertTime = question.GetMinExpertTime();
                qp.AvgExpertTime = question.GetAvgExpertTime();
                qp.MaxExpertTime = question.GetMaxExpertTime();
                qp.Hint = question.GetHint();

                data.Questions[i] = qp;
            }

            XmlSerializer serializer = new XmlSerializer(typeof(QuestionXMLData));
            serializer.Serialize(file, data);

            file.Close();
        }
#endif

        [Serializable]
        public class QuestionXMLData
        {
            public int Version = VERSION;
            public string TestQuestion;
            public QuestionParams[] Questions;
        }

        [Serializable]
        public class QuestionParams
        {
            public string InternalName;
            public float TimeAllowed;
            public float MinEasyTime;
            public float AvgEasyTime;
            public float MaxEasyTime;
            public float MinMediumTime;
            public float AvgMediumTime;
            public float MaxMediumTime;
            public float MinHardTime;
            public float AvgHardTime;
            public float MaxHardTime;
            public float MinExpertTime;
            public float AvgExpertTime;
            public float MaxExpertTime;
            public string Hint;
        }
        
        #endregion
    }
}
