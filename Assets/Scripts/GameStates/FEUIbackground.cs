using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GarageGames.Torque.Core;
using GarageGames.Torque.T2D;
using MathFreak.Math.Views;
using MathFreak.Math;
using GarageGames.Torque.Materials;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MathFreak.Math.Questions;
using MathFreak.Text;
using System.Diagnostics;
using MathFreak.GamePlay;
using MathFreak.AsyncTaskFramework;


namespace MathFreak.GameStates
{
    /// <summary>
    /// This isn't a gamestate, but this class will control the background that will show during all the 
    /// FEUI screens, from the title onwards.  It is a singleton class that allows loading, unloading, and
    /// setting properties of the FEUI background scene.
    /// </summary>
    public class FEUIbackground
    {
        private static FEUIbackground _instance;
        private bool _isLoaded;
        protected TorqueFolder _folder;

        public static FEUIbackground Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new FEUIbackground();
                }

                return _instance;
            }
        }

        private FEUIbackground()
        {
        }

        public void Load()
        {
            // if the background is already loaded then nothing to do here
            if (_isLoaded) return;

            // else we should create a folder to load the background into

            // remember the current folder so we can reset to that when we're done
            TorqueFolder prevActiveFolder = TorqueObjectDatabase.Instance.CurrentFolder;

            // create and register our own folder
            _folder = new TorqueFolder();
            _folder.Name = "folderFEUI_BG";

            // NOTE: need to register the folder before we can set it as the current folder
            // NOTE: register the new folder with the rootFolder as current so that it is not unregistered unless we want it to be (if we registered it with whatever folder happens to be current then when that folder [which probably belongs to another gamestate] is unregistered so will our folder be!)
            TorqueObjectDatabase.Instance.CurrentFolder = TorqueObjectDatabase.Instance.RootFolder;
            TorqueObjectDatabase.Instance.Register(_folder);
            TorqueObjectDatabase.Instance.CurrentFolder = _folder;

            // load the scene (will load into the folder we created)
            Game.Instance.LoadScene(@"data\levels\FEUIbackground.txscene");
            _isLoaded = true;

            // show version number
            TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("feui_versionnumber").Components.FindComponent<MathMultiTextComponent>().TextValue = Game.VERSION_NUMBER;
            TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("feui_versionnumber").Visible = false;

            // TESTING

            //// checking logit function is working correctly
            //for (int i = 0; i < 100; i++)
            //{
            //    double rnd = Game.Instance.Rnd.NextDouble();
            //    float abstractAnswerTime = (float)System.Math.Log(rnd / (1 - rnd));

            //    // normalize the logit time to -1 to +1
            //    abstractAnswerTime /= 5.0f; // divide by five as most useful section of the logit function for us is the range -5 to +5

            //    if (abstractAnswerTime < -1.0f)
            //    {
            //        abstractAnswerTime = -1.0f;
            //    }
            //    else if (abstractAnswerTime > 1.0f)
            //    {
            //        abstractAnswerTime = 1.0f;
            //    }

            //    Debug.WriteLine("answertime: " + abstractAnswerTime);
            //}

            // testing math expression display

            // simple single element test
            T2DStaticSprite sprite = TorqueObjectDatabase.Instance.CloneObject<T2DStaticSprite>("mathtestsprite");
            sprite.Visible = false;

            //MathElementView.InitializeRenderSettings(((sprite.Material as SimpleMaterial).Texture.Instance as Texture2D).Format, new Vector4(1, 0, 1, 1));

            //MathVariableNumber testnum = new MathVariableNumber(39);

            //MathElementView testview = testnum.GenerateView(50, 2, ResourceManager.Instance.LoadFont(@"data\fonts\Math Freak Font copy").Instance);

            //sprite.Size = new Vector2(testview.Texture.Width, testview.Texture.Height);
            //(sprite.Material as SimpleMaterial).SetTexture(testview.Texture);
            //TorqueObjectDatabase.Instance.Register(sprite);

            //// test an actual expression
            //MathExpression expression = MathExpression.Parse("2 - ( ( 4 * 3 ) / 25 ^ 2 + 12 ) ^ 3 + 8 * 1");
            //T2DStaticSprite expressionSprite = expression.GenerateView(70, 5, ResourceManager.Instance.LoadFont(@"data\fonts\Math Freak Font copy").Instance, new Vector4(0.5f, 0.5f, 0.8f, 0.9f));
            //expressionSprite.Position = new Vector2(-640.0f + 50.0f + (expressionSprite.Size.X * 0.5f), 370.0f - 50.0f - (expressionSprite.Size.Y * 0.5f));
            //TorqueObjectDatabase.Instance.Register(expressionSprite);

            //// test an algebra expression
            //MathExpression expression = MathExpression.Parse("1 * y");
            //expression.Simplify_EvaluateNumericalExpressions();
            ////expression.Simplify_AddExponents();
            ////expression.Simplify_SubtractExponents();
            ////expression.Simplify_CombineIdenticalTermsInAddition();
            ////expression.Simplify_CombineIdenticalTermsInSubtraction();
            ////expression.Simplify_MoveNumbersToLeftOfMultiplicationExpressions();
            //T2DStaticSprite expressionSprite = expression.GenerateSprite(70, 5, Game.GetEnumeratedFont(Game.EnumMathFreakFont.MathQuestion), new Vector4(0.5f, 0.5f, 0.8f, 0.9f));
            //expressionSprite.Position = new Vector2(-640.0f + 50.0f + (expressionSprite.Size.X * 0.5f), 370.0f - 50.0f - (expressionSprite.Size.Y * 0.5f));
            //TorqueObjectDatabase.Instance.Register(expressionSprite);

            //// test displaying special symbols (except the limits arrow)
            //MathExpression expression = MathExpression.Parse("4 / 2 + 4 // 2 + 4 /// 2 + 4 * 2 + 8 ROOT 3");
            //Texture2D texture = expression.GenerateTexture(60, 5, Game.GetEnumeratedFont(Game.EnumMathFreakFont.MathAnswer), new Vector4(0.0f, 0.0f, 0.5f, 1.0f)).Texture;
            //sprite.Size = new Vector2(texture.Width, texture.Height);
            //(sprite.Material as SimpleMaterial).SetTexture(texture);
            //sprite.Position = new Vector2(-640.0f + 50.0f + (sprite.Size.X * 0.5f), 370.0f - 50.0f - (sprite.Size.Y * 0.5f));
            //TorqueObjectDatabase.Instance.Register(sprite);

            // TESTING ends

            // DEBUG - game is crashing after playing about 16 questions - crashes on creating a rendertarget - so we are creating a crash-test bed here that will replicate it automatically outside of the gameplay so also ruling out other factors
            //Game.Instance.AddAsyncTask(AsyncTask_RenderTargetCrashTest(), true);
            //TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("feui_questiondisplay").Visible = false;
            // DEBUG

            // set the active folder back to the previously active one
            TorqueObjectDatabase.Instance.CurrentFolder = prevActiveFolder;
        }

        private IEnumerator<AsyncTaskStatus> AsyncTask_RenderTargetCrashTest()
        {
            QuestionDealer dealer = new QuestionDealer(1);
            int counter = 0;

            while (true)
            {
                counter++;

                //if (counter % 10 == 0)
                //Debug.WriteLine("count: " + counter);

                TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("feui_questiondisplay").Components.FindComponent<MathMultiTextComponent>().TextValue = "(" + counter + ") #" + dealer.GetQuestion().Question;
                //RenderTarget2D target = Util.CreateRenderTarget2D(Game.Instance.GraphicsDevice, 700, 500, 1, SurfaceFormat.Color);
                //RenderTarget2D target = Util.CreateRenderTarget2D(Game.Instance.GraphicsDevice, 128, 128, 1, SurfaceFormat.Color);
                //RenderTarget2D target = Util.CreateRenderTarget2D(Game.Instance.GraphicsDevice, 1048, 1048, 1, SurfaceFormat.Color);

                float elapsedTime = 0.0f;

                while (elapsedTime < 0.2f)
                {
                    elapsedTime += Game.Instance.dt;
                    yield return null;
                }

                //target.GetTexture().Dispose();
                //target.Dispose();

                //if (counter % 20 == 0)
                //{
                //GC.Collect();
                //}
            }
        }

        public void Unload()
        {
            if (_isLoaded)
            {
                Game.Instance.UnloadScene(@"data\levels\FEUIbackground.txscene");
                _isLoaded = false;
            }
        }

        public void SetRayColour()
        {
        }

        public static void PlayMusic()
        {
            MFSoundManager.Instance.PlayMusic(MFSoundManager.FEUI_MUSIC);
        }
    }
}
