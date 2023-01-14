using System;
using System.Diagnostics;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;
using Microsoft.Xna.Framework.GamerServices;

using GarageGames.Torque.Core;
using GarageGames.Torque.SceneGraph;
using GarageGames.Torque.Sim;
using GarageGames.Torque.GameUtil;
using GarageGames.Torque.T2D;
using GarageGames.Torque.Platform;

using MathFreak.GameStateFramework;
using MathFreak.GameStates;
using MathFreak.GameStates.Dialogs;
using MathFreak.GamePlay;
using MathFreak.GUIFrameWork;
using MathFreak.AsyncTaskFramework;
using MathFreak.Math;
using Microsoft.Xna.Framework.Content;
using GarageGames.Torque.XNA;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Net;
using GarageGames.Torque.Core.Xml;
using MathFreak.ContentLoading;
using MathFreak.Highscores;
using MathFreak.Math.Questions;
using Microsoft.Xna.Framework.Media;



namespace MathFreak
{
    public class Game : TorqueGame
    {
        // DEBUG/PROFILING
        // increment this for testing purposes - so both parties can know which version they are running (should typically be running identical versions)
        public const string VERSION_NUMBER = "0.816A";
        //public int FpsCounter;
        //private const float FPS_COUNTER_DELAY = 0.5f;   // how often the fps counter display will be updated (in seconds)
        //private float _fpsCounterUpdateDelay = 0.0f;
        //private int _frameCount = 0;

        //======================================================
        #region Static methods, fields, constructors

        public GamePlaySettings LastChampionshipSettings;
        public GamePlaySettings LastLocalSettings;
        public GamePlaySettingsMultiplayerLIVE LastLIVESettings;
        public bool[][] CustomMathGradeSelections;

        public static Game Instance
        {
            get { return _myGame; }
        }
        
        // for stuff that wants to 'tick' elapsed time, but doesn't otherwise have access to a dt variable (could really just remove all the other ref passing of dt and just have stuff use this in future - unless dt needs to be modified on a per 'ticker' basis then we only need one global really)
        public float dt
        {
            get { return _dt; }
        }

        public static SpriteBatch SpriteBatch
        {
            get { return _spriteBatch; }
        }

        // Enumerating the font allows them to be selected easily in the scene editor when a component
        // takes a font name as a param, as well as just generally making the specifying of fonts less
        // distributed and more managable.
        //
        // NOTE: enumerated fonts are mapped to actual fonts and font names via the GetEnumeratedFont()
        // and GetEnumeratedFontName() methods
        public enum EnumMathFreakFont { Default, DialogText, MenuButton, MathQuestion, MathAnswer, PlayerName, PlayerXXXXWins, LevelName, QuestionType, Score, ScoreFloater };

//#if XBOX
        public StorageDevice XBOXStorageDevice
        {
            get { return _storageDevice; }
        }

        public String XBOXDataContainer
        {
            get { return "Math Fighter - Data"; }
        }

        /// <summary>
        /// we can't separately store gamer specific setting and gamer agnostic settings without showing two device chooser dialogs (ugly!)
        /// so we will save player specific stuff in differently named files.
        /// </summary>
        public String XBOXGamerFileString
        {
            get { return _gamer.Gamertag; } // atm we return gamertag, but if that proves to not work for all possible tags then we can change it later - e.g. hash the tag to a filename friendly form
        }

        public String Gamertag
        {
            get { return _gamer.Gamertag; }
        }

        public Texture2D GamerPic
        {
            get
            {
                try
                {
                    return _gamer.GetProfile().GamerPicture;
                }
                catch
                {
                    // it's possible to get a gamer privilege acception here
                    return null;
                }
            }
        }

        public SignedInGamer PrimaryGamer
        {
            get { return _gamer; }
        }

        public int RichPresenceScore
        {
            set { _gamer.Presence.PresenceValue = value; }
        }

        public PlayerIndex XNAPlayerIndex
        {
            get { return (PlayerIndex)_playerIndex; }
        }
//#else
//        public String Gamertag
//        {
//            get { return "A. Player"; }
//        }
//#endif

        public int GamepadPlayerIndex
        {
            get { return _playerIndex; }
        }

        public override bool IsReallyActive
        {
            get
            {               
                return true;
            }
        }

        public Random Rnd
        {
            get { return _rnd; }
        }

        public GamePlaySettings ActiveGameplaySettings
        {
            get { return _activeGamePlaySettings; }
            set { _activeGamePlaySettings = value; }
        }

        public ContentBlock SharedContent
        {
            get { return _sharedContent; }
        }

        public ContentBlock SplashContent
        {
            get { return _splashContent; }
        }

        #endregion

        //======================================================
        #region Constructors
        #endregion

        //======================================================
        #region Public properties, operators, constants, and enums
        #endregion

        //======================================================
        #region Public Methods

        public static void Main()
        {
            Debug.WriteLine("Math Freak - version " + VERSION_NUMBER);            
            Debug.WriteLine("Memory usage at game start: " + GC.GetTotalMemory(true));

            _myGame = new Game();

            // begin the game.  Further setup is done in BeginRun()
            _myGame.Run();
        }

        public void OnActiveGamepadDetected(int playerIndex)
        {
            if (_playerIndex == -1)
            {
                Debug.WriteLine("Detected player as index: " + playerIndex);
                _playerIndex = playerIndex;

//#if !XBOX
//                MFSoundManager.Instance.LoadSettings();
//                FEUIbackground.PlayMusic();
//                HighscoreData.Instance.Load();
//                LoadGameplaySettings();
//#endif

                MFSoundManager.Instance.PlaySFXImmediately(MFSoundManager.EnumSFX.StartButton);

                GameStateManager.Instance.ClearAndLoad(GameStateNames.MAINMENU, GameState.RESET_LASTSELECTED);
            }
        }

        public void RegisterWaiting(String id)
        {
            Assert.Fatal(!_waitList.Contains(id), "Duplicate task added to wait list");

            _waitList.Add(id);
        }

        public void UnRegisterWaiting(String id)
        {
            _waitList.Remove(id);
        }

        public TorqueSceneData LoadScene(String name)
        {
            TorqueSceneData ret = SceneLoader.Load(name);

            //// could be more than one camera if more than one scene loaded - so just do all of them
            //List<T2DSceneCamera> cameras = TorqueObjectDatabase.Instance.FindObjects<T2DSceneCamera>();

            //foreach (T2DSceneCamera cam in cameras)
            //{
            //    cam.Extent = new Vector2(1280.0f, 720.0f);
            //    Debug.WriteLine("aspect ratio: " + cam.CameraAspectRatio);
            //}

            // check graphics resolution is correct
            Debug.WriteLine("Loaded scene and...\nScreen resolution is: " + GraphicsDevice.DisplayMode.Width + " x " + GraphicsDevice.DisplayMode.Height);

            // check cameras are set up correctly
            List<T2DSceneCamera> cameras = TorqueObjectDatabase.Instance.FindObjects<T2DSceneCamera>();

            foreach (T2DSceneCamera cam in cameras)
            {
                Debug.WriteLine("camera extents: " + cam.Extent + " and aspect ratio: " + cam.CameraAspectRatio);
            }

            return ret;
        }

        public void UnloadScene(String name)
        {
            SceneLoader.Unload(name);
        }

        public void TellAFriend()
        {
#if XBOX
            Guide.ShowComposeMessage(XNAPlayerIndex, "Hey, try Math Fighter out under Xbox 360 Indie games. Its awesome and you can challenge me online!", null);
#endif
        }

        public PlayerLocal GetLocalPlayer()
        {
            return new PlayerLocal(_playerIndex);
        }

        public SignedInGamer FindSignedInGamer(string gamertag)
        {
            foreach (SignedInGamer signedInGamer in SignedInGamer.SignedInGamers)
            {
                if (signedInGamer.Gamertag == gamertag) return signedInGamer;
            }

            return null;
        }

        public static string GetEnumeratedFontName(EnumMathFreakFont font)
        {
            // TODO: these need refactoring so the names match up with actual usage - over time things have strayed (will also require updating the scene files and schema though)
            switch (font)
            {
                case EnumMathFreakFont.Default:
                    return "menubuttonfont";
                    //break;

                case EnumMathFreakFont.MenuButton:
                    return "Main Font";
                    //break;

                case EnumMathFreakFont.MathQuestion:
                    return "Questions Font";
                //break;

                case EnumMathFreakFont.MathAnswer:
                    return "Answers";
                //break;

                case EnumMathFreakFont.DialogText:
                    return "Questions Font";
                //break;

                case EnumMathFreakFont.PlayerName:
                    return "Player Name";
                //break;

                case EnumMathFreakFont.PlayerXXXXWins:
                    return "Player XXXX Wins";
                //break;

                case EnumMathFreakFont.QuestionType:
                    return "Mid Panel text";
                //break;

                case EnumMathFreakFont.Score:
                    return "Score";
                //break;

                case EnumMathFreakFont.ScoreFloater:
                    return "Floating answer";
                //break;

                case EnumMathFreakFont.LevelName:
                    return "Mid Panel text";
                //break;

                default:
                    Assert.Fatal(false, "Unrecognized font: " + font);
                    return GetEnumeratedFontName(EnumMathFreakFont.Default);
                    //break;
            }
        }

        public static SpriteFont GetEnumeratedFont(EnumMathFreakFont font)
        {
            return ResourceManager.Instance.LoadFont(@"data/fonts/" + GetEnumeratedFontName(font)).Instance;
        }

        public void AddAsyncTask(IEnumerator<AsyncTaskStatus> task, bool startImmediately)
        {
            AsyncTaskManager.Instance.AddTask(task, TASKLIST_GAMEUPDATE, startImmediately);
        }

        #endregion

        //======================================================
        #region Private, protected, internal methods

        protected override void SetupGraphics()
        {
            base.SetupGraphics();

            Debug.WriteLine("Setting up graphics - forcing 720p");

            _graphicsManager.PreferredBackBufferWidth = 1280;
            _graphicsManager.PreferredBackBufferHeight = 720;
        }

        /// <summary>
        /// Called after the graphics device is created and before the game is about to start running.
        /// </summary>
        protected override void BeginRun()
        {
            base.BeginRun();

            if (Game.ExportSchema) return;  // if TX is building it's schema then we're not really running the game so do nothing...

            // initialize math framework
            MathExpression.RegisterDefaultOperators();
            //MathQuestion.SaveData();  // use this if needing to generate a fresh xml file otherwise leave this line commented out or the existing data will get overwritten.
            MathQuestion.LoadData();

            // initialize some global graphics resources
            _spriteBatch = new SpriteBatch(TorqueEngineComponent.Instance.Game.GraphicsDevice);
            LRTPool.Instance.Init();

            //// TESTING BEGINS
            //// testing math expression stuff

            //string expressionString = "2 + 3";
            //MathExpression expression = MathExpression.Parse(expressionString);
            //Debug.WriteLine("expression: " + expressionString + " => evaluates to: " + expression.Evaluate());
            //Assert.Fatal(expression.TextRepresentation().Trim() == expressionString, "Unit test failed - expression string is corrupted: " + expression.TextRepresentation().Trim());
            //Assert.Fatal(expression.Evaluate().Value == 5, "Unit test failed!");

            //expressionString = "2 - 3";
            //expression = MathExpression.Parse(expressionString);
            //Debug.WriteLine("expression: " + expressionString + " => evaluates to: " + expression.Evaluate());
            //Assert.Fatal(expression.TextRepresentation().Trim() == expressionString, "Unit test failed - expression string is corrupted: " + expression.TextRepresentation().Trim());
            //Assert.Fatal(expression.Evaluate().Value == -1, "Unit test failed!");

            //expressionString = "5 - 3 - 2";
            //expression = MathExpression.Parse(expressionString);
            //Debug.WriteLine("expression: " + expressionString + " => evaluates to: " + expression.Evaluate());
            //Assert.Fatal(expression.TextRepresentation().Trim() == expressionString, "Unit test failed - expression string is corrupted: " + expression.TextRepresentation().Trim());
            //Assert.Fatal(expression.Evaluate().Value == 0, "Unit test failed!");

            //expressionString = "5 - 3 + 2";
            //expression = MathExpression.Parse(expressionString);
            //Debug.WriteLine("expression: " + expressionString + " => evaluates to: " + expression.Evaluate());
            //Assert.Fatal(expression.TextRepresentation().Trim() == expressionString, "Unit test failed - expression string is corrupted: " + expression.TextRepresentation().Trim());
            //Assert.Fatal(expression.Evaluate().Value == 4, "Unit test failed!");

            //expressionString = "2 * 3";
            //expression = MathExpression.Parse(expressionString);
            //Debug.WriteLine("expression: " + expressionString + " => evaluates to: " + expression.Evaluate());
            //Assert.Fatal(expression.TextRepresentation().Trim() == expressionString, "Unit test failed - expression string is corrupted: " + expression.TextRepresentation().Trim());
            //Assert.Fatal(expression.Evaluate().Value == 6, "Unit test failed!");

            //expressionString = "12 / 3";
            //expression = MathExpression.Parse(expressionString);
            //Debug.WriteLine("expression: " + expressionString + " => evaluates to: " + expression.Evaluate());
            //Assert.Fatal(expression.TextRepresentation().Trim() == expressionString, "Unit test failed - expression string is corrupted: " + expression.TextRepresentation().Trim());
            //Assert.Fatal(expression.Evaluate().Value == 4, "Unit test failed!");

            //expressionString = "2 * 3 + 4 * 5";
            //expression = MathExpression.Parse(expressionString);
            //Debug.WriteLine("expression: " + expressionString + " => evaluates to: " + expression.Evaluate());
            //Assert.Fatal(expression.TextRepresentation().Trim() == expressionString, "Unit test failed - expression string is corrupted: " + expression.TextRepresentation().Trim());
            //Assert.Fatal(expression.Evaluate().Value == 26, "Unit test failed!");

            //expressionString = "12 / 3 + 4 * 5";
            //expression = MathExpression.Parse(expressionString);
            //Debug.WriteLine("expression: " + expressionString + " => evaluates to: " + expression.Evaluate());
            //Assert.Fatal(expression.TextRepresentation().Trim() == expressionString, "Unit test failed - expression string is corrupted: " + expression.TextRepresentation().Trim());
            //Assert.Fatal(expression.Evaluate().Value == 24, "Unit test failed!");

            //expressionString = "2 ^ 4";
            //expression = MathExpression.Parse(expressionString);
            //Debug.WriteLine("expression: " + expressionString + " => evaluates to: " + expression.Evaluate());
            //Assert.Fatal(expression.TextRepresentation().Trim() == expressionString, "Unit test failed - expression string is corrupted: " + expression.TextRepresentation().Trim());
            //Assert.Fatal(expression.Evaluate().Value == 16, "Unit test failed!");

            //expressionString = "-2 ^ 4";
            //expression = MathExpression.Parse(expressionString);
            //Debug.WriteLine("expression: " + expressionString + " => evaluates to: " + expression.Evaluate());
            //Assert.Fatal(expression.TextRepresentation().Trim() == expressionString, "Unit test failed - expression string is corrupted: " + expression.TextRepresentation().Trim());
            //Assert.Fatal(expression.Evaluate().Value == 16, "Unit test failed!");

            //expressionString = "-2 ^ 3";
            //expression = MathExpression.Parse(expressionString);
            //Debug.WriteLine("expression: " + expressionString + " => evaluates to: " + expression.Evaluate());
            //Assert.Fatal(expression.TextRepresentation().Trim() == expressionString, "Unit test failed - expression string is corrupted: " + expression.TextRepresentation().Trim());
            //Assert.Fatal(expression.Evaluate().Value == -8, "Unit test failed!");

            //expressionString = "10 - 2 ^ 3";
            //expression = MathExpression.Parse(expressionString);
            //Debug.WriteLine("expression: " + expressionString + " => evaluates to: " + expression.Evaluate());
            //Assert.Fatal(expression.TextRepresentation().Trim() == expressionString, "Unit test failed - expression string is corrupted: " + expression.TextRepresentation().Trim());
            //Assert.Fatal(expression.Evaluate().Value == 2, "Unit test failed!");

            //expressionString = "10 - -2 ^ 3";
            //expression = MathExpression.Parse(expressionString);
            //Debug.WriteLine("expression: " + expressionString + " => evaluates to: " + expression.Evaluate());
            //Assert.Fatal(expression.TextRepresentation().Trim() == expressionString, "Unit test failed - expression string is corrupted: " + expression.TextRepresentation().Trim());
            //Assert.Fatal(expression.Evaluate().Value == 18, "Unit test failed!");

            //expressionString = "4 * 3 * 2";
            //string subExpressionString = "+ 5 ";
            //expression = MathExpression.Parse(expressionString);
            //expression.InsertBefore((expression.ExpressionTree.LeftParam as MathOperator), subExpressionString);
            //Debug.WriteLine("expression: " + expressionString + " has sub-expression " + subExpressionString + " inserted using InsertBefore()");
            //Debug.WriteLine("....expression: " + expression.TextRepresentation() + " => evaluates to: " + expression.Evaluate());
            //Assert.Fatal(expression.TextRepresentation() == "4 * 3 + 5 * 2 ", "Unit test failed on insertion!");
            //Assert.Fatal(expression.Evaluate().Value == 22, "Unit test failed!");

            //expressionString = "4 * 5 * 2";
            //subExpressionString = "3 + ";
            //expression = MathExpression.Parse(expressionString);
            //expression.InsertAfter(((expression.ExpressionTree.LeftParam as MathOperator).LeftParam as MathOperator), subExpressionString);
            //Debug.WriteLine("expression: " + expressionString + " has sub-expression " + subExpressionString + " inserted using InsertAfter()");
            //Debug.WriteLine("....expression: " + expression.TextRepresentation() + " => evaluates to: " + expression.Evaluate());
            //Assert.Fatal(expression.TextRepresentation() == "4 * 3 + 5 * 2 ", "Unit test failed on insertion!");
            //Assert.Fatal(expression.Evaluate().Value == 22, "Unit test failed!");

            //expressionString = "4 + 5 - 2";
            //subExpressionString = "3 + ";
            //expression = MathExpression.Parse(expressionString);
            //expression.InsertAfter(((expression.ExpressionTree.LeftParam as MathOperator).LeftParam as MathOperator), subExpressionString);
            //Debug.WriteLine("expression: " + expressionString + " has sub-expression " + subExpressionString + " inserted using InsertAfter()");
            //Debug.WriteLine("....expression: " + expression.TextRepresentation() + " => evaluates to: " + expression.Evaluate());
            //Assert.Fatal(expression.TextRepresentation() == "4 + 3 + 5 - 2 ", "Unit test failed on insertion!");
            //Assert.Fatal(expression.Evaluate().Value == 10, "Unit test failed!");

            //expressionString = "4 + 3 - 2";
            //subExpressionString = "+ 5";    // also tests auto-appending the trailing space that is required in any expression string that is being inserted into the expression tree
            //expression = MathExpression.Parse(expressionString);
            //expression.InsertBefore((expression.ExpressionTree.LeftParam as MathOperator), subExpressionString);
            //Debug.WriteLine("expression: " + expressionString + " has sub-expression " + subExpressionString + " inserted using InsertBefore()");
            //Debug.WriteLine("....expression: " + expression.TextRepresentation() + " => evaluates to: " + expression.Evaluate());
            //Assert.Fatal(expression.TextRepresentation() == "4 + 3 + 5 - 2 ", "Unit test failed on insertion!");
            //Assert.Fatal(expression.Evaluate().Value == 10, "Unit test failed!");

            //expressionString = "5 - ( 3 - 2 )";
            //expression = MathExpression.Parse(expressionString);
            //Debug.WriteLine("expression: " + expressionString + " => evaluates to: " + expression.Evaluate());
            //Assert.Fatal(expression.TextRepresentation().Trim() == expressionString, "Unit test failed - expression string is corrupted: " + expression.TextRepresentation().Trim());
            //Assert.Fatal(expression.Evaluate().Value == 4, "Unit test failed!");

            //expressionString = "( 5 - 3 ) * 2";
            //expression = MathExpression.Parse(expressionString);
            //Debug.WriteLine("expression: " + expressionString + " => evaluates to: " + expression.Evaluate());
            //Assert.Fatal(expression.TextRepresentation().Trim() == expressionString, "Unit test failed - expression string is corrupted: " + expression.TextRepresentation().Trim());
            //Assert.Fatal(expression.Evaluate().Value == 4, "Unit test failed!");

            //expressionString = "( 15 - 3 ) ^ 2 + 4 * 2";
            //expression = MathExpression.Parse(expressionString);
            //Debug.WriteLine("expression: " + expressionString + " => evaluates to: " + expression.Evaluate());
            //Assert.Fatal(expression.TextRepresentation().Trim() == expressionString, "Unit test failed - expression string is corrupted: " + expression.TextRepresentation().Trim());
            //Assert.Fatal(expression.Evaluate().Value == 152, "Unit test failed!");

            //expressionString = "( 4 + 3 ) - 2";
            //subExpressionString = "* ( 2 + 5 )";    // also tests auto-appending the trailing space that is required in any expression string that is being inserted into the expression tree
            //expression = MathExpression.Parse(expressionString);
            //expression.InsertAfter((expression.ExpressionTree.LeftParam as MathOperator).LeftParam, subExpressionString);
            //Debug.WriteLine("expression: " + expressionString + " has sub-expression " + subExpressionString + " inserted using InsertAfter()");
            //Debug.WriteLine("....expression: " + expression.TextRepresentation() + " => evaluates to: " + expression.Evaluate());
            //Assert.Fatal(expression.TextRepresentation() == "( 4 + 3 ) * ( 2 + 5 ) - 2 ", "Unit test failed on insertion: " + expression.TextRepresentation());
            //Assert.Fatal(expression.Evaluate().Value == 47, "Unit test failed!");

            //expressionString = "2 + ( ( 4 * 8 ) / 4 ^ 2 ) ^ 3 * 8 - 1";
            //expression = MathExpression.Parse(expressionString);
            //Debug.WriteLine("expression: " + expressionString + " => evaluates to: " + expression.Evaluate());
            //Assert.Fatal(expression.TextRepresentation().Trim() == expressionString, "Unit test failed - expression string is corrupted: " + expression.TextRepresentation().Trim());
            //Assert.Fatal(expression.Evaluate().Value == 65, "Unit test failed!");

            //// TESTING ENDS

            // initialize gamestates
            GameStateManager.Instance.Add(GameStateNames.LOADCONTENT, new GameStateLoadContent());
            GameStateManager.Instance.Add(GameStateNames.SPLASH_iBLADE, new GameStateSplash_iBlade());
            GameStateManager.Instance.Add(GameStateNames.SPLASH_TX, new GameStateSplash_TX());
            GameStateManager.Instance.Add(GameStateNames.SPLASH_INTRO_VID, new GameStateSplash_introVid());
            GameStateManager.Instance.Add(GameStateNames.TITLE, new GameStateTitle());
            GameStateManager.Instance.Add(GameStateNames.MAINMENU, new GameStateMainMenu());
            GameStateManager.Instance.Add(GameStateNames.GAMEPLAY_TEACHME, new GameStateGameplayTeachMe());
            GameStateManager.Instance.Add(GameStateNames.GAMEPLAY_FREAK, new GameStateGameplayFreak());
            GameStateManager.Instance.Add(GameStateNames.GAMEPLAY_CHAMPIONSHIP, new GameStateGameplayChampionship());
            GameStateManager.Instance.Add(GameStateNames.GAMEPLAY_MULTIPLAYER_LOCAL, new GameStateGameplayMultiplayerLocal());
            GameStateManager.Instance.Add(GameStateNames.GAMEPLAY_MULTIPLAYER_LIVE, new GameStateGameplayMultiplayerLIVE());
            GameStateManager.Instance.Add(GameStateNames.OPTIONS, new GameStateOptions());
            GameStateManager.Instance.Add(GameStateNames.SINGLEPLAYER, new GameStateSinglePlayer());
            GameStateManager.Instance.Add(GameStateNames.FREAK, new GameStateFreak());
            GameStateManager.Instance.Add(GameStateNames.SETTINGS_SINGLEPLAYER, new GameStateSettingsSinglePlayer());
            GameStateManager.Instance.Add(GameStateNames.SETTINGS_MULTIPLAYER_LOCAL, new GameStateSettingsMultiPlayerLocal());
            GameStateManager.Instance.Add(GameStateNames.SETTINGS_MULTIPLAYER_LIVE, new GameStateSettingsMultiPlayerLIVE());
            GameStateManager.Instance.Add(GameStateNames.SETTINGS_MULTIPLAYER_LIVEinfo, new GameStateSettingsMultiPlayerLIVEinfo());
            GameStateManager.Instance.Add(GameStateNames.CHARACTERSELECTION_SINGLEPLAYER, new GameStateCharacterSelectionLobbySingleplayer());
            GameStateManager.Instance.Add(GameStateNames.CHARACTERSELECTION_MULTIPLAYERLOCAL, new GameStateCharacterSelectionLobbyMultiplayerLocal());
            GameStateManager.Instance.Add(GameStateNames.CHARACTERSELECTION_MULTIPLAYERLIVE, new GameStateCharacterSelectionLobbyMultiplayerLIVE());
            GameStateManager.Instance.Add(GameStateNames.VS_SPLASH_SINGLEPLAYER, new GameStateVsSplashSinglePlayer());
            GameStateManager.Instance.Add(GameStateNames.VS_SPLASH_MULTIPLAYERLOCAL, new GameStateVsSplashMultiplayerLocal());
            GameStateManager.Instance.Add(GameStateNames.VS_SPLASH_MULTIPLAYERLIVE, new GameStateVsSplashMultiplayerLIVE());
            GameStateManager.Instance.Add(GameStateNames.MULTIPLAYER, new GameStateMultiPlayer());
            GameStateManager.Instance.Add(GameStateNames.LOCALMATCH, new GameStateLocalMatch());
            GameStateManager.Instance.Add(GameStateNames.XBOXLIVE, new GameStateXboxLIVE());
            GameStateManager.Instance.Add(GameStateNames.QUICKMATCH, new GameStateQuickMatch());
            GameStateManager.Instance.Add(GameStateNames.CHOOSEMATCH, new GameStateChooseMatch());
            //GameStateManager.Instance.Add(GameStateNames.CREATEMATCH, new GameStateCreateMatch());
            GameStateManager.Instance.Add(GameStateNames.HIGHSCORES, new GameStateHighScores());
            GameStateManager.Instance.Add(GameStateNames.HIGHSCORES_SINGLEPLAYER, new GameStateHighscoresSinglePlayer());
            GameStateManager.Instance.Add(GameStateNames.HIGHSCORES_MULTIPLAYER, new GameStateHighscoresMultiplayer());
            GameStateManager.Instance.Add(GameStateNames.HOW_TO_PLAY, new GameStateHowToPlay());
            GameStateManager.Instance.Add(GameStateNames.LOBBY, new GameStateLobby());
            GameStateManager.Instance.Add(GameStateNames.PAUSEMENU, new GameStatePauseMenu());
            GameStateManager.Instance.Add(GameStateNames.WON_CHAMPIONSHIP, new GameStateWonChampionship());
            GameStateManager.Instance.Add(GameStateNames.CREDITS, new GameStateCredits());
            GameStateManager.Instance.Add(GameStateNames.JOIN_INVITED, new GameStateJoinInvited());

            GameStateManager.Instance.Add(GameStateNames.DIALOG_QUIT, new DialogQuit());
            GameStateManager.Instance.Add(GameStateNames.DIALOG_NOTIFICATION, new DialogNotification());
            GameStateManager.Instance.Add(GameStateNames.DIALOG_QUITGAMEPLAY, new DialogQuitGameplay());
            GameStateManager.Instance.Add(GameStateNames.DIALOG_QUITCHARACTERSELECTION, new DialogQuitCharacterSelection());
            GameStateManager.Instance.Add(GameStateNames.DIALOG_QUITSETTINGSLOBBY, new DialogQuitSettingsLobby());
            GameStateManager.Instance.Add(GameStateNames.DIALOG_KICKPLAYERS, new DialogKickPlayers());
            GameStateManager.Instance.Add(GameStateNames.DIALOG_QUITBACKTOCHAMPIONSHIP, new DialogQuitBackToChampionship());
            GameStateManager.Instance.Add(GameStateNames.DIALOG_REPLAYCHAMPIONSHIPMATCH, new DialogReplayChampionshipMatch());

            // initialize sound, networking, and the global async task list
            MFSoundManager.Instance.Init();
            NetworkSessionManager.Instance.Init();
            AsyncTaskManager.Instance.NewTaskList(TASKLIST_GAMEUPDATE);

            // content blocks
            SetupContentBlocks();
            _sharedContent.AllowLoading();  // so that globalvars scene (and any other scene) can load the few things it needs - but we aren't preloading all the shared content yet; we'll do that once the splashscreens kickoff
            _splashContent.AllowLoading();  // splash screen content is allowed to load - we will unload it once the splashscreens have completed

            // load the global vars scene (it will remain loaded for the entire game)
            LoadScene(@"data\levels\GlobalVars.txscene");

            //// TESTING - for rapid testing purposes we won't be showing the splashscreens
            //Game.Instance.CreateContentLoader("Default", true);
            //contentLoader = Game.Instance.ContentLoaders["Default"];

            //contentLoader.RegisterAsset("data/effects/ClockRevealEffect");
            //contentLoader.RegisterAsset("data/effects/OffsetTextureEffect");
            //contentLoader.RegisterAsset("data/effects/RadialGradientEffect");
            //contentLoader.RegisterAsset("data/effects/VerticalGradientEffect");
            //contentLoader.RegisterAsset("data/effects/TeleportEffect");
            //contentLoader.LoadAssets(new string[8] { "data/images/", "data/images/Tutors/", "data/images/CharacterSelectionLobby/", "data/images/HUD/", "data/images/TitleScreen/", "data/images/UI/", "data/images/Misc/", "data/images/VsScreen/" }, new bool[8] { false, false, false, false, false, false, false, false }, new string[1] { "data/fonts/" }, new bool[1] { false });

            //GameStateManager.Instance.ClearAndLoad(GameStateNames.LOADCONTENT, null);
            //// TESTING

            // kick the game off with the splash screens
            GameStateManager.Instance.ClearAndLoad(GameStateNames.SPLASH_iBLADE, null);
        }

        private void SetupContentBlocks()
        {
            // set up shared content block that will load the assets that persist for the entire game
            _sharedContent = new ContentBlock();
            _sharedContent.AddAsset<Texture2D>("data/images/GGLogo");
            _sharedContent.AddFolder<Texture2D>("data/images/Tutors/", false);
            _sharedContent.AddFolder<Texture2D>("data/images/CharacterSelectionLobby/", false);
            _sharedContent.AddFolder<Texture2D>("data/images/HUD/", false);
            _sharedContent.AddFolder<Texture2D>("data/images/TitleScreen/", false);
            _sharedContent.AddFolder<Texture2D>("data/images/UI/", false);
            _sharedContent.AddFolder<Texture2D>("data/images/Misc/", false);
            _sharedContent.AddFolder<Texture2D>("data/images/VsScreen/", false);
            _sharedContent.AddFolder<SpriteFont>("data/fonts/", false);
            _sharedContent.AddFolder<Effect>("data/effects/", false);

            // set up a content block for the splashscreens - we will unload this content when the splashscreens have completed
            _splashContent = new ContentBlock();
            _splashContent.AddAsset<Texture2D>("data/images/splash_tx_hd_720p");
            _splashContent.AddFolder<Video>("data/video/", false);
        }

        protected override void Draw(GameTime gameTime)
        {
            // DEBUG - update FPS counter

            //_frameCount++;

            //if (_fpsCounterUpdateDelay >= FPS_COUNTER_DELAY)
            //{
            //    FpsCounter = (int)(_frameCount / _fpsCounterUpdateDelay);

            //    _frameCount = 0;
            //    _fpsCounterUpdateDelay = 0;
            //}

            // DEBUG ENDS

            PreDrawManager.Instance.PreDraw(dt);

            base.Draw(gameTime);

            //avatarRenderer.Draw(avatarAnimation.BoneTransforms,
            //    avatarAnimation.Expression);
        }

        protected override void Update(GameTime gameTime)
        {
            NetworkSessionManager.Instance.Tick();  // first things first, tick the networking stuff so that any active networking session is bang up to date before we do anything else

            GUIManager.Instance.PreTick();  // gui manager needs to do some stuff before we tick the input processes

            base.Update(gameTime);  // allow all torque stuff to update - including the gamepad/keyboard input

//#if XBOX
//            if (avatarRenderer.IsLoaded)
//            {
//                avatarAnimation.Update(gameTime.ElapsedGameTime, true);
//            } 
//#endif
            
            AsyncTaskManager.Instance.Tick(TASKLIST_GAMEUPDATE);

            if (_waitList.Count > 0)
            {
                return; // waiting for something to complete - such as selecting the storage device on xbox 360 (selecting the storage device is executed asynchronously, but we want the game to pause while the device is being selected)
            }

//#if XBOX
            //// check for gamepad disconnected
            //if (_playerIndex != -1 && !GamePad.GetState(XNAPlayerIndex).IsConnected)
            //{
            //    //return; // pause until the gamepad is reconnected
            //}

            // if the playerindex has been detected but not yet got the gamer info
            if (_playerIndex != -1 && _gamer == null)
            {
                // find the gamer in list of signed in gamers
                foreach (SignedInGamer signedInGamer in SignedInGamer.SignedInGamers)
                {
                    if (signedInGamer.PlayerIndex == XNAPlayerIndex)
                    {
                        _gamer = signedInGamer;
                        break;
                    }
                }

                // if signed in the get info
                if (_gamer != null)
                {
                    //_profile = _gamer.GetProfile();
                    //_gamer.Presence.PresenceMode = GamerPresenceMode.Score;
                    //_gamer.Presence.PresenceValue = 0;
                }
                // else get the player to sign in
                else
                {
                    // check if already showing the guide before we try showing the sign-in dialog
                    if (!Guide.IsVisible)
                    {
                        Guide.ShowSignIn(1, false);
                    }

                    return; // pause all other processing until the player has signed in
                }
            }

            // no storage device selected? then get the player to select one
            if (_storageDevice == null)
            {
                if (_playerIndex != -1)
                {
                    AsyncTaskManager.Instance.AddTask(AsyncTask_GetTaskStorageDeviceSelector(), TASKLIST_GAMEUPDATE, true);
                    return; // game will be paused while the storage device is chosen
                }
            }
            else if (!_storageDevice.IsConnected)
            {
                // storage device was disconnected - prompt for selecting a storage device
                _storageDevice = null;
                AsyncTaskManager.Instance.AddTask(AsyncTask_GetTaskStorageDeviceSelector(), TASKLIST_GAMEUPDATE, true);
                return; // game will be paused while the storage device is chosen
            }
//#endif

            // now tick our stuff
            _dt = gameTime.ElapsedGameTime.Milliseconds / 1000.0f;

            // DEBUG - FPS
            //_fpsCounterUpdateDelay += _dt;
            // END DEBUG - FPS

            GameStateManager.Instance.Tick(dt);
            MFSoundManager.Instance.Tick(dt);
            HighscoresP2P.Instance.Tick(dt);
        }

//#if XBOX
        private IEnumerator<AsyncTaskStatus> AsyncTask_GetTaskStorageDeviceSelector()
        {
            bool pauseGame = !GameStateGameplay.IsPlaying;

            // tell the game to pause while we do our stuff
            if (pauseGame)
            {
                RegisterWaiting("StorageDeviceSelector");
            }

            // wait for a moment while the start button sound plays
            float elapsed = 0.0f;

            while (elapsed < 1.0f)
            {
                elapsed += _dt;
                yield return null;
            }

            // make sure the guide is not showing before we show the dialog
            while (Guide.IsVisible) yield return null;

            // show the storage device selector dialog
            IAsyncResult result = Guide.BeginShowStorageDeviceSelector(null, null);

            // wait until the dialog exits
            while (!result.IsCompleted) yield return null;

            // close the dialog
            _storageDevice = Guide.EndShowStorageDeviceSelector(result);

            // tell the game we have finished doing our stuff (so the game will unpause if there are no other tasks to wait for)
            if (pauseGame)
            {
                UnRegisterWaiting("StorageDeviceSelector");
            }

            // load various settings and other data
            if (_storageDevice != null)
            {
                AddAsyncTask(AsyncTask_LoadStuffAndPlayMusic(), true);
            }
        }
//#endif

        private IEnumerator<AsyncTaskStatus> AsyncTask_LoadStuffAndPlayMusic()
        {
            RegisterWaiting("loadstuff");

            // display message while loading stuff
            TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("feui_loadingmessage").Visible = true;

            // wait for the guide to disappear - or it will stay visible while we load the scores and the player will think the game has hung
            while (Guide.IsVisible) yield return null;

            // play music
            MFSoundManager.Instance.LoadSettings();
            FEUIbackground.PlayMusic();

            // load stuff
            LoadGameplaySettings();
            HighscoreData.Instance.Load();

            // hide the loading message
            TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("feui_loadingmessage").Visible = false;

            // kick off P2P highscores (or try to at least)
            HighscoresP2P.Instance.Start();

            UnRegisterWaiting("loadstuff");
        }

        public void ExitTheGame()
        {
            // do any stuff we need to do before the game exits (this is a proper controlled exit as opposed to the game being externally told to stop)
            MFSoundManager.Instance.StopMusic();

            // actually exit
            Exit();
        }

        // NOTE: this is called just before the game finally exits and will be triggered
        // regardless of what caused the game to exit (assuming the game hasn't crashed obviously)
        protected override void OnExiting(object sender, EventArgs args)
        {
            // save stuff
            SaveGameplaySettings();
            HighscoreData.Instance.Save(true);

            // some debug info
            Debug.WriteLine("Memory usage at game exit: " + GC.GetTotalMemory(false));

            // dispose stuff
            LRTPool.Instance.Dispose();

            base.OnExiting(sender, args);
        }

        public void SaveGameplaySettings()
        {
            if (LastChampionshipSettings != null) LastChampionshipSettings.Save("championsettings.mfs");
            if (LastLocalSettings != null) LastLocalSettings.Save("localsettings.mfs");
            if (LastLIVESettings != null) LastLIVESettings.Save("LIVEsettings.mfs");
            GamePlaySettings.SaveMathGradeSelections(CustomMathGradeSelections, "custommathgradesel.mfs");
        }

        public void LoadGameplaySettings()
        {
            LastChampionshipSettings = new GamePlaySettings();
            LastLocalSettings = new GamePlaySettings();
            LastLIVESettings = new GamePlaySettingsMultiplayerLIVE();

            if (!LastChampionshipSettings.Load("championsettings.mfs")) LastChampionshipSettings = null;
            if (!LastLocalSettings.Load("localsettings.mfs")) LastLocalSettings = null;
            if (!LastLIVESettings.Load("LIVEsettings.mfs")) LastLIVESettings = null;
            CustomMathGradeSelections = GamePlaySettings.LoadMathGradeSelections("custommathgradesel.mfs");

            if (CustomMathGradeSelections == null)
            {
                CustomMathGradeSelections = new bool[1][];
                CustomMathGradeSelections[0] = new bool[14] { false, false, false, false, false, false, false, false, false, false, false, false, false, false };
            }
        }

        public void OnInviteAccepted()
        {
            GameStateManager.Instance.ClearAndLoad(GameStateNames.JOIN_INVITED, null);
        }

        protected override void Initialize()
        {
            if (Game.ExportSchema)
            {
                base.Initialize();  // if TX is building it's schema then we're not really running the game so do nothing...
                return;
            }

//#if XBOX
            Components.Add(new GamerServicesComponent(this));
//#endif

            _rnd = new Random();

            base.Initialize();

            _waitList = new List<String>();

//#if XBOX
            //avatarDesc = AvatarDescription.CreateRandom();
            //avatarRenderer = new AvatarRenderer(avatarDesc, true);
            //avatarAnimation = new AvatarAnimation(AvatarAnimationPreset.Celebrate);

            //avatarRenderer.World =
            //    Matrix.CreateRotationY(MathHelper.ToRadians(180.0f));
            //avatarRenderer.Projection =
            //    Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(45.0f),
            //    GraphicsDevice.Viewport.AspectRatio, .01f, 200.0f);
            //avatarRenderer.View =
            //    Matrix.CreateLookAt(new Vector3(0, 1, 3), new Vector3(0, 1, 0),
            //    Vector3.Up);

            SignedInGamer.SignedOut += new EventHandler<SignedOutEventArgs>(OnGamerSignedOut);
//#endif
        }

//#if XBOX
        protected void OnGamerSignedOut(object sender, SignedOutEventArgs e)
        {
            // ignore this event if the gamer signing out is not the one playing the game
            if (_gamer == null || _gamer.Gamertag != e.Gamer.Gamertag) return;

            // else reset all the gamer and playerindex stuff, save the audio options, and
            // send the game back to the splash screen/player detection gamestate.
            MFSoundManager.Instance.StopMusic();

            _gamer = null;
            //_profile = null;
            _playerIndex = -1;
            _storageDevice = null;  // get the player to select the storage device again as devices can be locked to a particular gamertag 

            GameStateManager.Instance.ClearAndLoad(GameStateNames.TITLE, null);
        }
//#endif

        #endregion

        //======================================================
        #region Private, protected, internal fields

        static Game _myGame;

        private float _dt;  // elapsed time in seconds since last game tick (abbreviation 'dt' as in the math term)
        private static SpriteBatch _spriteBatch;

        private int _playerIndex = -1;

        private List<String> _waitList;
        private GamePlaySettings _activeGamePlaySettings;
        private Random _rnd;

//#if XBOX
        private StorageDevice _storageDevice;
        private SignedInGamer _gamer;
        //private GamerProfile _profile;
//#endif

        private const String TASKLIST_GAMEUPDATE = "gameupdate";

        private ContentBlock _sharedContent;
        private ContentBlock _splashContent;

        #endregion

//#if XBOX
//        AvatarDescription avatarDesc;
//        AvatarRenderer avatarRenderer;
//        AvatarAnimation avatarAnimation;
//#endif
    }
}