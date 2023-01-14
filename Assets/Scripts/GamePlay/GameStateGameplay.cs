using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

using GarageGames.Torque.GameUtil;
using GarageGames.Torque.Core;
using GarageGames.Torque.Materials;
//using GarageGames.Torque.SceneGraph;
using GarageGames.Torque.Sim;
//using GarageGames.Torque.GameUtil;
using GarageGames.Torque.T2D;
using GarageGames.Torque.Platform;

using MathFreak.GameStateFramework;
using MathFreak.GUIFrameWork;
using System.Threading;
using MathFreak.AsyncTaskFramework;
using MathFreak.GameStates;
using Microsoft.Xna.Framework.Graphics;
using MathFreak.Math.Categories;
using MathFreak.Text;
using System.Diagnostics;
using MathFreak.vfx;
using MathFreak.Math.Questions;



namespace MathFreak.GamePlay
{
    /// <summary>
    /// This state handles the base gameplay behaviour.  It provides all the core functionality for
    /// the gameplay.  It is multiplayer/singleplayer agnostic and network/local agnostic.  Specific
    /// gamestates will extend this one to modify they core functionality as required - e.g. displaying
    /// different messages, returning to different gamestates on exit, different win conditions, etc.
    /// 
    /// NOTE: this gamestate should not be registered with the gamestate manager; it is the shared
    /// base class that the specific gamestates will derive from, but not a gamestate that should
    /// be used directly.
    /// </summary>
    public abstract class GameStateGameplay : GameState
    {
        protected bool _isExiting;

        //private TextComponent _fpsText; // DEBUG

        // TESTING
        //private bool isQuestionPaused = false;
        //private float isQuestionPausedButtonPressDelay = 0.0f;
        // TESTING ends

        protected const float BASICDAMAGE = 10;
        protected const float SUPERDAMAGE = 20;
        //protected const int CRITICAL_HEALTH_LEVEL = 25;

        protected const String TASKLIST_GAMEPLAY_ASYNC_EVENTS = "gameplayasyncevents";
        private List<String> _waitList;
        protected float _dt;    // so async tasks can know the elapsed time
        protected TorqueFolder _sceneFolder;

        protected enum EnumGameplayState { Intro, QuestionIntro, QuestionDisplay, WaitForAnswer, AnswerCorrect, AnswerWrong, AnswerContinue, AnswerNotGiven, ShowCorrectAnswer, WaitingForSuperAttack, QuestionOutro, WaitingForQuestionContent, GameLost, GameWon, Outro };
        protected EnumGameplayState _state;

        protected GameplayData _gameplayData;

        protected bool _isShowingPauseMenu;
        protected bool _canShowPauseMenu;
        private InputMap _escStartInputMap;

        protected T2DStaticSprite _questionDisplay;
        protected T2DStaticSprite[] _answerDisplays;
        protected RenderMaterial[] _normalAnswerWindowMaterials;
        protected RenderMaterial[] _correctAnswerWindowMaterials;
        protected RenderMaterial[] _wrongAnswerWindowMaterials;
        protected RenderMaterial[] _showAnswerWindowMaterials;
        protected RenderMaterial[] _gamepadButtonMaterials;
        protected RenderMaterial _correctMaterial;
        protected RenderMaterial _wrongMaterial;
        private T2DSceneObject _hintshadow;
        protected T2DStaticSprite _playerScoreText;

        protected SpotlightComponent[] _spotlights;

        protected T2DAnimatedSprite[] _tutorSprites;

        public const int SUPERTRAILFX_MAXSEGMENTS = 6;
        public const int SUPERTRAILFX_GHOSTSPERSEGMENT = 2;
        public const int SUPERTRAILFX_NUMGHOSTS = SUPERTRAILFX_MAXSEGMENTS * SUPERTRAILFX_GHOSTSPERSEGMENT + SUPERTRAILFX_GHOSTSPERSEGMENT;
        protected T2DAnimatedSprite[][] _ghostSprites;

        protected ClockRevealComponent _timerReveal;
        protected float _countdownRemaining;
        protected bool _canPlayTimeRunningOutAnim;
        protected int[] _health = new int[2];
        protected int[] _healthDisplayed = new int[2];
        protected OffsetTextureComponent[] _healthbars = new OffsetTextureComponent[2];
        protected const float MULTIPLIER_MAX = 10.0f;
        protected const float MULTIPLIER_INC = 1.0f;
        protected float _multiplier;
        protected bool _isInMultiplierZone;
        protected int _sequenceScore; // after a run of answering questions correctly ends, we tell the player what that run scored so need to track that
        protected int _sequenceCount;
        protected int _questionNumber;
        protected bool[] _isFrozen = new bool[2];
        protected bool[] _visuallyUnfreezePlayers = new bool[2];
        protected bool _abortFreezingPlayers;
        protected int _playerScore;

        protected enum EnumAttackDecision { None, Normal, Super };
        protected EnumAttackDecision _attackDecision;

        protected int _answerGiven;     // used to keep track of the answer a player gave - carries this info across gamestate changes;
        protected int _activePlayer;    // used to track the active player across gameplay state changes - e.g. when a player presses a button to answer the next state needs to know which player did that
                                        // NOTE: this is the active player as in player 1 or player 2 and not the gamepad player index stuff.

        public static bool IsPlaying;  // true if this gamestate is active (some things need to know if the game is active or not - e.g. the storage device selector shouldn't pause the game if it's active).

        protected const int MAX_LIVES = 5;

        //private long PROFILING_MEM_USAGE;
        //private int PROFILING_GC_COLLECTION_COUNTER;
        //private WeakReference PROFILING_GC_COLLECTION_DETECTOR;


        public override void Init()
        {
            base.Init();

            AsyncTaskManager.Instance.NewTaskList(TASKLIST_GAMEPLAY_ASYNC_EVENTS);
            _waitList = new List<String>();
        }

        public override void PreTransitionOn(string paramString)
        {
            _waitList = new List<String>(); // make *sure* wait list starts off empty

            base.PreTransitionOn(paramString);

            _isExiting = false;
            IsPlaying = true;
            
            // note: the scene is loaded *after* transitioning on so that we don't clash with the scene that is transitioning off
        }

        public override void OnTransitionOnCompleted()
        {
            base.OnTransitionOnCompleted();

            // scene loading stuff
            FEUIbackground.Instance.Unload();
            Game.Instance.LoadScene(@"data\levels\HUD.txscene");
            Game.Instance.LoadScene(@"data\levels\HUD_ExtraTextures.txscene");

            // get references to various objects that we will be doing things with
            GetObjectReferences();

            // general initialization of the scene and it's objects as required
            InitializeScene();

            // initialize stuff based on gameplay settings
            InitializeGamePlay();

            // set the gameplay state to the game intro stuff (will display a 'get ready to start' message or some such)
            _state = EnumGameplayState.Intro;

            //GUIManager.Instance.ActivateGUI();

            // setup input map so that pause menu will show in response to escape key or start button
            _isShowingPauseMenu = false;
            _canShowPauseMenu = true;

            _escStartInputMap = new InputMap();

            int gamepadId = InputManager.Instance.FindDevice("gamepad" + Game.Instance.GamepadPlayerIndex);

            if (gamepadId >= 0)
            {
                _escStartInputMap.BindAction(gamepadId, (int)XGamePadDevice.GamePadObjects.Start, _OnShowPauseMenu);
            }

            int keyboardId = InputManager.Instance.FindDevice("keyboard");

            if (keyboardId >= 0)
            {
                _escStartInputMap.BindAction(keyboardId, (int)Keys.Escape, _OnShowPauseMenu);
            }

            InputManager.Instance.PushInputMap(_escStartInputMap);

            GC.Collect();
            //PROFILING_MEM_USAGE = GC.GetTotalMemory(false);
            //Debug.WriteLine("Gameplay ready to start - Memory usage: " + PROFILING_MEM_USAGE);
            //PROFILING_GC_COLLECTION_COUNTER = 0;
            //PROFILING_GC_COLLECTION_DETECTOR = new WeakReference(new Object());

            //// if no location is selected then choose one at random
            //if (Game.Instance.ActiveGameplaySettings.Location == TutorManager.EnumTutor.None)
            //{
            //    Assert.Warn(false, "No match 'location' specified!  Will pick a random one!");
            //    Game.Instance.ActiveGameplaySettings.Location = TutorManager.Instance.GetRandomTutor();
            //}

            // play the selected music
            TutorManager.Instance.PlayMusic(0, Game.Instance.ActiveGameplaySettings.LocationToUse);

            // show the selected bg - same character as for music
            SimpleMaterial bgMaterial = TutorManager.Instance.GetBG(0, Game.Instance.ActiveGameplaySettings.LocationToUse);
            Assert.Fatal(bgMaterial != null && bgMaterial.Texture.Instance != null, "Background texture not be loaded!!!\ntutor bg == " + Game.Instance.ActiveGameplaySettings.LocationToUse);
            TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("hud_bg").Material = bgMaterial;
        }

        public override void OnTransitionOffCompleted()
        {
            base.OnTransitionOffCompleted();

            DoUnload();
        }

        public override void OnSetAsForeground()
        {
            base.OnSetAsForeground();

            _isShowingPauseMenu = false;

            TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("hud_shadowpanel").Visible = false;
        }

        public override void UnloadedImmediately()
        {
            base.UnloadedImmediately();

            IsPlaying = false;

            DoUnload();
        }

        protected virtual void DoUnload()
        {
            //long currMemUsage = GC.GetTotalMemory(false);
            //Debug.WriteLine("Gameplay ending - Memory usage: " + currMemUsage);
            //Debug.WriteLine("                  Memory usage diff: " + (currMemUsage - PROFILING_MEM_USAGE));
            //Debug.WriteLine("                  GC collection count: " + PROFILING_GC_COLLECTION_COUNTER);

            // shutdown gui for this screen
            InputManager.Instance.PopInputMap(_escStartInputMap);
            
            // shutdown tasks and stuff
            KillAllTasks(true, true);
            MFSoundManager.Instance.StopAllSFX();

            // unload scene/s
            Game.Instance.UnloadScene(@"data\levels\HUD_ExtraTextures.txscene");
            Game.Instance.UnloadScene(@"data\levels\HUD.txscene");

            ReleaseObjectReferences();

            // unload assets used by the tutors
            GameStateVsSplash.UnloadContent();
        }

        public virtual void OnQuitting()
        {
            if (!_isExiting)
            {
                _isExiting = true;

                OnPreExiting();
                OnExiting();
                GameStateManager.Instance.ClearAndLoad(GameStateNames.MAINMENU, GameStateMainMenu.RETURNING_FROM_GAME);
            }
        }

        protected virtual void OnPreExiting()
        {
            KillAllTasks(true, true);
            RegisterWaiting("exiting gameplay");   // don't let any processing happen!
        }

        protected void OnExiting()
        {
            UnloadAssets();
            KillAllTasks(true, true);
            RegisterWaiting("exiting gameplay");   // don't let any processing happen!
        }

        protected void UnloadAssets()
        {
            // kill all tasks - don't want anything trying to grab assets after we have unloaded them.
            KillAllTasks(false, false);    // we can use false as the param and not worry about any tasks getting ticked as they will be marked for dispoal - trying to tick them would only result in them being disposed of anyway (all that 'true' does is to say dispose immediately - but we can't do that as UnloadAssets() is sometimes called from within an asynctask so it would mess up if we dispose all the tasks and the tasks are also in the middle of being enumerated over).

            // NOTE: we don't need to unload the tutor animations and stuff now as we cull the
            // tutor anims from all scene files that don't need them so there will be no errant
            // references to crash the game.

            //// shutdown and unload all the tutor sprites, ghosts, and overlays, so that we can safely unload the tutor animation assets
            //_tutorSprites[0].Visible = false;
            //_tutorSprites[0].PauseAnimation();
            //TorqueObjectDatabase.Instance.Unregister(_tutorSprites[0]);
            //_tutorSprites[1].Visible = false;
            //_tutorSprites[1].PauseAnimation();
            //TorqueObjectDatabase.Instance.Unregister(_tutorSprites[1]);

            //T2DAnimatedSprite overlay0 = TorqueObjectDatabase.Instance.FindObject<T2DAnimatedSprite>("hud_superfxanim0");
            //overlay0.Visible = false;
            //overlay0.PauseAnimation();
            //TorqueObjectDatabase.Instance.Unregister(overlay0);
            //T2DAnimatedSprite overlay1 = TorqueObjectDatabase.Instance.FindObject<T2DAnimatedSprite>("hud_superfxanim1");
            //overlay1.Visible = false;
            //overlay1.PauseAnimation();
            //TorqueObjectDatabase.Instance.Unregister(overlay1);

            //for (int i = 0; i < SUPERTRAILFX_NUMGHOSTS; i++)
            //{
            //    _ghostSprites[0][i].Visible = false;
            //    _ghostSprites[0][i].PauseAnimation();
            //    TorqueObjectDatabase.Instance.Unregister(_ghostSprites[0][i]);
            //    _ghostSprites[1][i].Visible = false;
            //    _ghostSprites[1][i].PauseAnimation();
            //    TorqueObjectDatabase.Instance.Unregister(_ghostSprites[1][i]);
            //}

            //_ghostSprites[0] = null;
            //_ghostSprites[1] = null;

            //// unload the assets
            //Game.Instance.RemoveContentLoader("TutorAnims");
        }

        public override bool CanAcceptInvite()
        {
            return !_isExiting;
        }

        public override void OnInviteAccepted()
        {
            base.OnInviteAccepted();
            _isExiting = true;
        }

        /// <summary>
        /// Called to get any object references that we need - e.g. sprites or components in the scene
        /// NOTE: remember to also release any object references when the level is unloaded (see: ReleaseObjectReferences())
        /// </summary>
        protected virtual void GetObjectReferences()
        {
            // grab references question and answer display objects
            _questionDisplay = TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("hud_questiondisplay");

            _answerDisplays = new T2DStaticSprite[4];
            _answerDisplays[0] = TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("hud_answerdisplay_0");
            _answerDisplays[1] = TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("hud_answerdisplay_1");
            _answerDisplays[2] = TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("hud_answerdisplay_2");
            _answerDisplays[3] = TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("hud_answerdisplay_3");

            // we need to get materials that we are going to swap around because the way the torque database
            // stores materials we can't garrauntee to get the ones that belong to our scene - which is
            // *not* good if there is another scene overlaying ours (i.e. in-game menu) at the time we go find
            // a material because that scene could get unloaded while we're still using the material!!!!!
            // So we get references to them right now, before any other scenes can overlay our scene.
            _normalAnswerWindowMaterials = new RenderMaterial[4];
            _correctAnswerWindowMaterials = new RenderMaterial[4];
            _wrongAnswerWindowMaterials = new RenderMaterial[4];
            _showAnswerWindowMaterials = new RenderMaterial[4];

            for (int i = 0; i < 4; i++)
            {
                _normalAnswerWindowMaterials[i] = TorqueObjectDatabase.Instance.FindObject<RenderMaterial>("hud_" + MapAnswerWindowIndexToLetter(i) + "NormalMaterial");
                _correctAnswerWindowMaterials[i] = TorqueObjectDatabase.Instance.FindObject<RenderMaterial>("hud_" + MapAnswerWindowIndexToLetter(i) + "CorrectMaterial");
                _wrongAnswerWindowMaterials[i] = TorqueObjectDatabase.Instance.FindObject<RenderMaterial>("hud_" + MapAnswerWindowIndexToLetter(i) + "WrongMaterial");
                _showAnswerWindowMaterials[i] = TorqueObjectDatabase.Instance.FindObject<RenderMaterial>("hud_" + MapAnswerWindowIndexToLetter(i) + "Right_AnsMaterial");
            }

            _gamepadButtonMaterials = new RenderMaterial[4];
            _gamepadButtonMaterials[0] = TorqueObjectDatabase.Instance.FindObject<RenderMaterial>("hud_Blue_Btn_NormalMaterial");
            _gamepadButtonMaterials[1] = TorqueObjectDatabase.Instance.FindObject<RenderMaterial>("hud_Yellow_Btn_NormalMaterial");
            _gamepadButtonMaterials[2] = TorqueObjectDatabase.Instance.FindObject<RenderMaterial>("hud_Green_Btn_NormalMaterial");
            _gamepadButtonMaterials[3] = TorqueObjectDatabase.Instance.FindObject<RenderMaterial>("hud_Red_Btn_NormalMaterial");

            _correctMaterial = TorqueObjectDatabase.Instance.FindObject<RenderMaterial>("CorrectMaterial");
            _wrongMaterial = TorqueObjectDatabase.Instance.FindObject<RenderMaterial>("WrongMaterial");

            // grab a reference to the tutor animation sprite and also cache the anims (the latter is VERY important - see comments in tutor manager for details)
            _tutorSprites = new T2DAnimatedSprite[2];
            _tutorSprites[0] = TorqueObjectDatabase.Instance.FindObject<T2DAnimatedSprite>("hud_tutor1");
            _tutorSprites[1] = TorqueObjectDatabase.Instance.FindObject<T2DAnimatedSprite>("hud_tutor2");
            TutorManager.Instance.CacheAnims();

            // grab references to the spotlights
            _spotlights = new SpotlightComponent[2];
            _spotlights[0] = TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("hud_spotlight0").Components.FindComponent<SpotlightComponent>();
            _spotlights[1] = TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("hud_spotlight1").Components.FindComponent<SpotlightComponent>();

            // grab a reference to the clock revealer component attached to the timer's outer ring object - we'll display the outer ring gradually as the time ticks by
            _timerReveal = TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("hud_timerring").Components.FindComponent<ClockRevealComponent>();

            // health bars
            _healthbars[0] = TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("hud_healthbar0").Components.FindComponent<OffsetTextureComponent>();
            _healthbars[1] = TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("hud_healthbar1").Components.FindComponent<OffsetTextureComponent>();

            // DEBUG - grab fps counter ref
            //_fpsText = TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("hud_fpstext").Components.FindComponent<TextComponent>();
            TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("hud_fpstext").Visible = false;

            // the shadowy box that displays the hint text
            _hintshadow = TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("hud_hintshadow");

            // player score display
            _playerScoreText = TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("HUD_score0");
        }

        protected virtual void ReleaseObjectReferences()
        {
            // question and answer display objects
            _questionDisplay = null;
            _answerDisplays = null;

            // materials
            _normalAnswerWindowMaterials = null;
            _correctAnswerWindowMaterials = null;
            _wrongAnswerWindowMaterials = null;
            _showAnswerWindowMaterials = null;

            _gamepadButtonMaterials = null;

            _correctMaterial = null;
            _wrongMaterial = null;

            // tutor animation sprites and release cached anims
            _tutorSprites = null;
            TutorManager.Instance.ReleaseCachedAnims();

            // spotlights
            _spotlights = null;

            // the clock revealer component attached to the timer's outer ring object - we'll display the outer ring gradually as the time ticks by
            _timerReveal = null;

            // health bars
            _healthbars[0] = null;
            _healthbars[1] = null;

            // DEBUG - fps counter ref
            //_fpsText = null;

            // the shadowy box that displays the hint text
            _hintshadow = null;

            // ghosts
            _ghostSprites = null;
        }

        /// <summary>
        /// Called to do any general initialization of the scene that has been loaded.
        /// e.g. some objects may need hiding initially.
        /// </summary>
        protected virtual void InitializeScene()
        {
            ShowQuestionAnswerTexts(false);
            ClearCountdownDisplay();

            _questionDisplay.Components.FindComponent<MathMultiTextComponent>().CharacterHeight = 60.0f;

            for (int i = 0; i < 4; i++)
            {
                _answerDisplays[i].Components.FindComponent<MathMultiTextComponent>().CharacterHeight = 60.0f;
            }

            // initialize and hide the popupmenu 'shadow panel'
            T2DStaticSprite shadowPanel = TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("hud_shadowpanel");
            SimpleMaterial material = shadowPanel.Material as SimpleMaterial;
            material.IsColorBlended = true;
            material.IsTranslucent = true;
            shadowPanel.Position = Vector2.Zero;
            shadowPanel.VisibilityLevel = 0.5f;
            shadowPanel.Visible = false;

            // hide messages
            TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("hud_messagetext").Visible = false;
            TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("hud_rightwrongmessage0").Visible = false;
            TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("hud_rightwrongmessage1").Visible = false;
            TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("hud_message_GETREADY").Visible = false;

            // hide level name and question info - we don't have question content yet so nothing to display there yet
            _gameplayData = null;
            UpdateLevelNameAndQuestionInfoDisplay();

            // hide positional markers
            //TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("hud_sequencefxpos0").Visible = false;
            //TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("hud_sequencefxpos1").Visible = false;

            // hide the super attack fx anims
            TorqueObjectDatabase.Instance.FindObject<T2DAnimatedSprite>("hud_superfxanim0").Visible = false;
            TorqueObjectDatabase.Instance.FindObject<T2DAnimatedSprite>("hud_superfxanim1").Visible = false;

            // note what Folder the scene is using
            _sceneFolder = TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("hud_message_GETREADY").Folder;

            // setup the hint shadow
            _hintshadow.Position = Vector2.Zero;
            _hintshadow.Visible = false;

            // health bars
            _healthbars[0].Offset = Vector2.One;
            _healthbars[1].Offset = Vector2.One;
            ((_healthbars[0].Owner as T2DStaticSprite).Material as SimpleMaterial).IsColorBlended = true;
            ((_healthbars[1].Owner as T2DStaticSprite).Material as SimpleMaterial).IsColorBlended = true;
            (_healthbars[0].Owner as T2DStaticSprite).ColorTint = Color.Yellow;
            (_healthbars[1].Owner as T2DStaticSprite).ColorTint = Color.Yellow;

            // create some ghost sprites that will be used for the super attack trail fx
            _ghostSprites = new T2DAnimatedSprite[2][];
            _ghostSprites[0] = new T2DAnimatedSprite[SUPERTRAILFX_NUMGHOSTS];
            _ghostSprites[1] = new T2DAnimatedSprite[SUPERTRAILFX_NUMGHOSTS];

            for (int i = SUPERTRAILFX_NUMGHOSTS - 1; i >= 0; i--)
            {
                int indx = SUPERTRAILFX_NUMGHOSTS - 1 - i;

                _ghostSprites[0][indx] = TorqueObjectDatabase.Instance.FindObject<T2DAnimatedSprite>("hud_tutor1").Clone() as T2DAnimatedSprite;
                _ghostSprites[1][indx] = TorqueObjectDatabase.Instance.FindObject<T2DAnimatedSprite>("hud_tutor2").Clone() as T2DAnimatedSprite;
                _ghostSprites[0][indx].Visible = false;
                _ghostSprites[1][indx].Visible = false;
                _ghostSprites[0][indx].Layer = _tutorSprites[0].Layer + 1;
                _ghostSprites[1][indx].Layer = _tutorSprites[1].Layer + 1;
                TorqueObjectDatabase.Instance.Register(_ghostSprites[0][indx]);
                TorqueObjectDatabase.Instance.Register(_ghostSprites[1][indx]);
            }

            // hide super attack prompts
            TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("hud_superattackprompt0").Visible = false;
            TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("hud_superattackprompt1").Visible = false;

            // hide player score by default
            _playerScoreText.Visible = false;

            TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("hud_debug_correctbutton").Visible = false;
        }

        /// <summary>
        /// Called to initialize the gameplay (loading settings or whatever needs doing) before moving
        /// to the first gameplay state.
        /// </summary>
        protected virtual void InitializeGamePlay()
        {
            Game.Instance.ActiveGameplaySettings.OnGameStarted();

            // no players?
            Assert.Fatal(Game.Instance.ActiveGameplaySettings.Players.Count > 0, "Trying to start a game with no players!");

            if (Game.Instance.ActiveGameplaySettings.Players.Count == 0)
            {
                // something really bad happened - ABORT!
                GameStateManager.Instance.ClearAndLoad(GameStateNames.MAINMENU, GameState.RESET_LASTSELECTED);
                return;
            }

            // setup the gamer pic/s and gamertag/s
            T2DStaticSprite[] sprites = new T2DStaticSprite[2];
            sprites[0] = TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("hud_gamerpic1");
            sprites[1] = TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("hud_gamerpic2");
            List<Player> players = Game.Instance.ActiveGameplaySettings.Players;

            for (int i = 0; i < players.Count; i++)
            {
                // gamer pic
                Texture2D gamerPic = players[i].GamerPic;

                if (gamerPic != null)
                {
                    SimpleMaterial material = new SimpleMaterial();
                    material.SetTexture(gamerPic);
                    sprites[i].Material = material;
                }

                // gamertag
                TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("hud_playername" + i).Components.FindComponent<MathMultiTextComponent>().TextValue = players[i].GamerTag;
            }

            // hook the player objects up to some input sources
            SetupPlayerInput();

            // initialize the gameplay data instance that we'll be updating throughout the game
            _gameplayData = InitializeGameplayData();

            // initialize health bar
            _health[0] = Game.Instance.ActiveGameplaySettings.EnergyBar;
            _health[1] = Game.Instance.ActiveGameplaySettings.EnergyBar;
            _healthDisplayed[0] = _health[0];
            _healthDisplayed[1] = _health[1];
            _multiplier = 1.0f;
            _sequenceCount = 0;

            // create a question dealer
            Game.Instance.ActiveGameplaySettings.CreateNewDealer();

            // starting from question count of zero
            _questionNumber = 0;

            // set tutor idle anim to the correct one so the right tutor is showing straight away
            AddAsyncTask(TutorManager.Instance.PlayAnim(0, Game.Instance.ActiveGameplaySettings.Players[0].Character, TutorManager.Tutor.EnumAnim.Idle, _tutorSprites[0]), true);
            AddAsyncTask(TutorManager.Instance.PlayAnim(1, Game.Instance.ActiveGameplaySettings.Players[1].Character, TutorManager.Tutor.EnumAnim.Idle, _tutorSprites[1]), true);

            // players obviously are not frozen at the start of the game
            _isFrozen[0] = false;
            _isFrozen[1] = false;

            // initialize score
            _playerScore = 0;
            UpdatePlayerScoreDisplay();
        }

        /// <summary>
        /// Derived classes must override this method to set up the player input stuff.
        /// </summary>
        protected virtual void SetupPlayerInput()
        {
            // for each player, if they are local then hook them up to the corresponding input component
            List<Player> players = Game.Instance.ActiveGameplaySettings.Players;

            for (int i = 0; i < players.Count; i++)
            {
                if (players[i] is PlayerLocal)
                {
                    PlayerInputComponent inputComponent = TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("hud_playerinput_" + i).Components.FindComponent<PlayerInputComponent>();
                    inputComponent.SetupInputMap(Game.Instance.ActiveGameplaySettings.Players[i] as PlayerLocal);
                }
            }
        }

        protected virtual GameplayData InitializeGameplayData()
        {
            GameplayData gpd = new GameplayData();

            // initialize each player's gameplay data
            gpd.InitializePlayer(0);
            gpd.InitializePlayer(1);

            return gpd;
        }

        protected void AddAsyncTask(IEnumerator<AsyncTaskStatus> task, bool startImmediately)
        {
            AsyncTaskManager.Instance.AddTask(task, TASKLIST_GAMEPLAY_ASYNC_EVENTS, startImmediately);
        }

        protected void KillAllTasks(bool disposeTasksImmediately, bool clearWaitList)
        {
            AsyncTaskManager.Instance.KillAllTasks(TASKLIST_GAMEPLAY_ASYNC_EVENTS, disposeTasksImmediately);

            if (clearWaitList)
            {
                _waitList.Clear();
            }

            // also we will hide any fx animation stuff that is tied to tasks and should thus be hidden when all the tasks are killed, or they will hang around statically onscreen (if necessary we could split the tasklist into multiple tasklists so we can have some 'fire and forget' stuff that we don't bother killing except when finally unloading the gamestate - for now though a single list will suffice).
            TorqueObjectDatabase.Instance.FindObject<T2DAnimatedSprite>("hud_superfxanim0").Visible = false;
            TorqueObjectDatabase.Instance.FindObject<T2DAnimatedSprite>("hud_superfxanim1").Visible = false;
        }

        public override void Tick(float dt)
        {
            base.Tick(dt);

            _dt = dt;

            // tick the async events list
            AsyncTaskManager.Instance.Tick(TASKLIST_GAMEPLAY_ASYNC_EVENTS);

            // DEBUG/PROFILING
            //if (!PROFILING_GC_COLLECTION_DETECTOR.IsAlive)
            //{
            //    //Trace.WriteLine("A garbage collection occurred!");
            //    PROFILING_GC_COLLECTION_DETECTOR = new WeakReference(new object());
            //    PROFILING_GC_COLLECTION_COUNTER++;
            //}

            //_fpsText.TextValue = Game.Instance.FpsCounter.ToString();
            //_fpsText.TextValue = TorqueBase.ObjectCount.ToString() + "/" + TorqueBase.LoadedObjectCount.ToString() + "/" + TorqueSafePtr<TorqueBase>.ReferencedObjects;
            //_fpsText.TextValue = TorqueBase.ObjectCount.ToString() + "/" + TextMaterial.Counter + "/" + TextMaterial.Undisposed;
            // DEBUG/PROFILING - ends

            // if there's anything that wants us to wait until it completes then just return
            if (_waitList.Count > 0) return;

            // else do some gameplay state processing
            ProcessState();
        }

        protected virtual void ProcessState()
        {
            // TESTING
            //// player can press the right stick to set the multiplier instantly to 10x (will not show onscreen)
            //for (PlayerIndex index = PlayerIndex.One; index <= PlayerIndex.Four; index++)
            //{
            //    GamePadState gps = GamePad.GetState(index);

            //    if (gps.IsButtonDown(Buttons.DPadUp))
            //    {
            //        _multiplier = MULTIPLIER_MAX;
            //        _multiplierShouldIncrease = true;
            //        break;
            //    }
            //}

            //// player can pause the question presentation so we can ponder the question being presented
            //isQuestionPausedButtonPressDelay += _dt;

            //if (isQuestionPausedButtonPressDelay > 1.0f)
            //{
            //    if (Game.Instance.ActiveGameplaySettings.Players[0].FreezePressed)
            //    {
            //        isQuestionPaused = !isQuestionPaused;
            //        isQuestionPausedButtonPressDelay = 0.0f;
            //    }
            //}

            //if (isQuestionPaused) return;
            // TESTING ENDS

            // update the player input (see note on the gameplaydata class)
            _gameplayData.UpdatePlayerInput(_countdownRemaining);

            switch (_state)
            {
                case EnumGameplayState.Intro:
                    ProcessIntro();
                    break;

                case EnumGameplayState.QuestionIntro:
                    ProcessQuestionIntro();
                    break;

                case EnumGameplayState.QuestionDisplay:
                    ProcessQuestionDisplay();
                    break;

                case EnumGameplayState.WaitForAnswer:
                    ProcessWaitingForAnswer();
                    break;

                case EnumGameplayState.AnswerCorrect:
                    ProcessAnswerCorrect();
                    break;

                case EnumGameplayState.AnswerWrong:
                    ProcessAnswerWrong();
                    break;

                case EnumGameplayState.AnswerContinue:
                    ProcessAnswerContinue();
                    break;

                case EnumGameplayState.AnswerNotGiven:
                    ProcessAnswerNotGiven();
                    break;

                case EnumGameplayState.ShowCorrectAnswer:
                    ProcessShowCorrectAnswer();
                    break;

                case EnumGameplayState.WaitingForSuperAttack:
                    ProcessWaitingForSuperAttack();
                    break;

                case EnumGameplayState.QuestionOutro:
                    ProcessQuestionOutro();
                    break;

                case EnumGameplayState.WaitingForQuestionContent:
                    ProcessWaitingForQuestionContent();
                    break;

                case EnumGameplayState.GameLost:
                    ProcessGameLost();
                    break;

                case EnumGameplayState.GameWon:
                    ProcessGameWon();
                    break;

                case EnumGameplayState.Outro:
                    ProcessOutro();
                    break;
            }
        }

        protected virtual void ProcessIntro()
        {
            _state = EnumGameplayState.WaitingForQuestionContent;

            // before we move to the next state show the 'get ready' message
            AddAsyncTask(AsyncTask_ShowMessage_GetReady(), true);
        }

        protected virtual void ProcessQuestionIntro()
        {
            if (_isExiting) return; // if we are exiting then don't show anymore questions

            _questionNumber++;
            _canPlayTimeRunningOutAnim = true;

            // show a 'next question' message before moving to the next state which will actually display the question
            _state = EnumGameplayState.QuestionDisplay;
            AddAsyncTask(AsyncTask_ShowMessage_NextQuestion(_questionNumber), true);
        }

        protected virtual void ProcessQuestionDisplay()
        {
            // setup the display objects with the question content
            _questionDisplay.Components.FindComponent<MathMultiTextComponent>().TextValue = _gameplayData.Question.Question;

            for (int i = 0; i < 4; i++)
            {
                _answerDisplays[i].Components.FindComponent<MathMultiTextComponent>().TextValue = _gameplayData.Question.Answers[i];
            }

            // make display objects visible
            ShowQuestionAnswerTexts(true);

            // allow the players to attempt answering the question and reset their input states
            for (int i = 0; i < Game.Instance.ActiveGameplaySettings.Players.Count; i++)
            {
                Game.Instance.ActiveGameplaySettings.Players[i].CanAnswer = true;
                Game.Instance.ActiveGameplaySettings.Players[i].ResetButtonPressed();
            }

            _gameplayData.ResetPlayerInput();
            InitializeCountdown();

            // wait for the player/s to try and answer the question
            _state = EnumGameplayState.WaitForAnswer;

            //// garbage collect after question display stuff has run - we wouldn't normally do an explicit garbage collection, but the math text rendering creates a lot of RenderTarget2D instances over a period of many questions and it seems that we need to clear these up completely (i.e GC them so some kind of internal GPU resource stuff is released) or the game will crash as it will no longer be able to create any more RenderTarget2D instances
            //Game.Instance.ScheduleGarbageCollection();
        }

        protected virtual void ProcessWaitingForAnswer()
        {
            bool processCountdown = true;   // we'll set this to false if anyone had a go at answering - timer should pause/stop when someone answers

            // TESTING
            //bool pressedCheat = false;
            // TESTING ENDS

            // we show a hint if any local player presses for it so keep track of whether anyone is requesting it
            int hintPressCount = 0;

            // only let one player answer at a time
            bool aPlayerAlreadyAnswered = false;

            // for each player check to see if they attempted to answer the question
            List<Player> players = Game.Instance.ActiveGameplaySettings.Players;

            for (int i = 0; i < players.Count; i++)
            {
                Player player = players[i];

                // if the player is local and the pause menu is showing then ignore and reset their input
                if (_isShowingPauseMenu && (player is PlayerLocal))
                {
                    player.ResetButtonPressed();
                    continue;
                }

                // TESTING
                // if the player is local then lshoulder determines whether to show/hide the right answer
                //if (player is PlayerLocal)
                //{
                //    // if the leftshoulder button hasn't already been activated then show/hide the right answer
                //    if (!pressedCheat)
                //    {
                //        TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("hud_debug_correctbutton").Visible = player.CheatPressed;
                //        pressedCheat = player.CheatPressed;
                //    }
                //}
                // TESTING ENDS

                // if player selected a blank answer then ignore that response
                if (IsAnAnswerButton(_gameplayData.PlayerInput[i]) && _gameplayData.IsBlankAnswer(MapGamepadButtonToAnswerWindowIndex(_gameplayData.PlayerInput[i])))
                {
                    player.ResetButtonPressed();
                }
                // else if the player pressed a button and isn't frozen out then decide what to do about it
                else if (!aPlayerAlreadyAnswered && player.CanAnswer && !_isFrozen[i] && IsAnAnswerButton(_gameplayData.PlayerInput[i]))
                {
                    aPlayerAlreadyAnswered = true;

                    // store player as the active player so next gamestate will know who just caused stuff to occur
                    _activePlayer = i;

                    // store the answer given so the next gamestate will know what answer was given (we aren't relying on looking this up for the active player because network sync can muck that up on rare occasions that a players input gets reset during a 'transition' point - e.g. the 'waitandresetinput' type task could potentially do this)
                    _answerGiven = MapGamepadButtonToAnswerWindowIndex(_gameplayData.PlayerInput[i]);

                    // this player can no longer answer - they've used up their chance on the current question now
                    player.CanAnswer = false;

                    // pause the timer
                    processCountdown = false;

                    // go to the appropriate gameplay state depending on whether they got the answer right or wrong
                    if (MapGamepadButtonToAnswerWindowIndex(_gameplayData.PlayerInput[i]) == _gameplayData.Question.RightAnswer)
                    {
                        _state = EnumGameplayState.AnswerCorrect;
                    }
                    else
                    {
                        _state = EnumGameplayState.AnswerWrong;
                    }
                }

                // check if player wants to (and can) show a hint
                if ((player is PlayerLocal) && !_isShowingPauseMenu && _gameplayData.HintPressed[i])
                {
                    hintPressCount++;
                }

                // if the player pressed hint then try and freeze them
                if (_gameplayData.HintPressed[i])
                {
                    FreezePlayer(i);
                }
                
                // if player pressed the 'taunt' button and no other animations are playing already, and not frozen, then do some taunting
                if (_gameplayData.TauntPressed[i] && !_tutorSprites[i].IsAnimationPlaying && !_isFrozen[i])
                {
                    TutorManager.Instance.PlayTauntSFX(i, Game.Instance.ActiveGameplaySettings.Players[i].Character);
                    AddAsyncTask(TutorManager.Instance.PlayAnim(i, Game.Instance.ActiveGameplaySettings.Players[i].Character, TutorManager.Tutor.EnumAnim.Taunt, _tutorSprites[i]), true);
                }
            }

            //// play hint sfx if a player requested hint and hint isn't already showing
            //if (hintPressCount > 0 && !_hintshadow.Visible)
            //{
            //    MFSoundManager.Instance.PlaySFX(MFSoundManager.EnumSFX.Hint);
            //}

            // show hint if it was requested by any player
            _hintshadow.Visible = (hintPressCount > 0);

            if (processCountdown)
            {
                ProcessCountdown();
            }
        }

        protected virtual void ProcessAnswerCorrect()
        {
            // get the appropriate answer window sprite and change it's material to the 'correct answer' material
            T2DStaticSprite answerWindow = TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("hud_answerwindow_" + _answerGiven);
            answerWindow.Material = _correctAnswerWindowMaterials[_answerGiven];

            // do the fx for getting the answer correct
            DoFX_AnswerCorrect(_activePlayer);

            // stop any timer sfx that are playing
            StopTimerSFX();

            // do an attack - what attack we do and it's timing will depend on the combo multiplier
            if (_multiplier > 1)
            {
                DoFX_Combo(_activePlayer, _answerGiven, _multiplier);
                _state = EnumGameplayState.WaitingForSuperAttack;
            }
            else
            {
                DoNormalAttack(_activePlayer, _multiplier);
                _state = EnumGameplayState.QuestionOutro;
                AddAsyncTask(AsyncTask_Wait(2), true);
            }
        }

        protected virtual void ProcessAnswerWrong()
        {
            // get the appropriate answer window sprite and change it's material to the 'wrong answer' material
            T2DStaticSprite answerWindow = TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("hud_answerwindow_" + _answerGiven);
            answerWindow.Material = _wrongAnswerWindowMaterials[_answerGiven];

            // multiplier gets reset
            ResetMultiplierAfterWrongAnswer();

            // do the fx for getting the answer wrong
            DoFX_AnswerWrong(_activePlayer);

            // if all players have answered then after a short delay we'll show what the right answer was
            if (AllPlayersAnswered())
            {
                // both players attack each other
                DoDoubleNormalAttack();

                // stop any timer sfx that are playing
                StopTimerSFX();

                _state = EnumGameplayState.ShowCorrectAnswer;
                AddAsyncTask(AsyncTask_Wait(1.5f), true);
            }
            // else there are still players who haven't had a shot at answering the question yet then we'll let them have a go (after a short delay)
            else
            {
                // move to the next state with no delay
                _state = EnumGameplayState.AnswerContinue;
                //AddAsyncTask(AsyncTask_Wait(1.5f), true);
            }
        }

        protected virtual void ProcessAnswerContinue()
        {
            // reset the highlighting
            //ResetAnswerWindowHighlighting();

            // move to the next state right away - don't reset player input or anything; if the other player was pressing then they will answer too so tough luck if they got it wrong too; they pressed already :)
            _state = EnumGameplayState.WaitForAnswer;
            //AddAsyncTask(AsyncTask_WaitAndResetPlayerInput(0.5f), true);
        }

        protected virtual void ProcessAnswerNotGiven()
        {
            // set all the answer windows WRONG
            TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("hud_answerwindow_0").Material = _wrongAnswerWindowMaterials[0];
            TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("hud_answerwindow_1").Material = _wrongAnswerWindowMaterials[1];
            TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("hud_answerwindow_2").Material = _wrongAnswerWindowMaterials[2];
            TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("hud_answerwindow_3").Material = _wrongAnswerWindowMaterials[3];

            // multiplier gets reset
            ResetMultiplier();

            // do the fx for not answering the question
            DoFX_AnswerNotGiven();

            // both players attack each other
            DoDoubleNormalAttack();

            // show the correct answer after a short delay
            _state = EnumGameplayState.ShowCorrectAnswer;
            AddAsyncTask(AsyncTask_Wait(1.5f), true);   // note: this is a minimum delay - the clock explosion will delay things, but here we specify a minimum independent of that fx
        }

        protected virtual void ProcessShowCorrectAnswer()
        {
            ResetAnswerWindowHighlighting();

            // get the appropriate answer window sprite and change it's material to the 'show answer' material
            int answerWindowIndex = _gameplayData.Question.RightAnswer;
            T2DStaticSprite answerWindow = TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("hud_answerwindow_" + answerWindowIndex);
            answerWindow.Material = _showAnswerWindowMaterials[answerWindowIndex];

            // move to the outro state after a short delay
            _state = EnumGameplayState.QuestionOutro;
            AddAsyncTask(AsyncTask_Wait(1.5f), true);
        }

        /// <summary>
        /// Waits for a while to see if the player decides to use their super attack.
        /// Will execute the player's normal attck if they don't choose to use the super attack.
        /// </summary>
        protected virtual void ProcessWaitingForSuperAttack()
        {
            switch (_attackDecision)
            {
                case EnumAttackDecision.None:
                    // just keep on waiting...
                    break;

                case EnumAttackDecision.Normal:
                    DoNormalAttack(_activePlayer, _multiplier);
                    _state = EnumGameplayState.QuestionOutro;   // goto question outro after attack completes (the attack will pause state processing)
                    //AddAsyncTask(AsyncTask_Wait(2), true);
                    break;

                case EnumAttackDecision.Super:
                    DoSuperAttack(_activePlayer, _multiplier);
                    _state = EnumGameplayState.QuestionOutro;   // goto question outro after attack completes (the attack will pause state processing)
                    //AddAsyncTask(AsyncTask_Wait(2), true);
                    break;

                default:
                    Assert.Fatal(false, "Unrecognized attack decision: " + _attackDecision);
                    break;
            }
        }

        protected virtual void ProcessQuestionOutro()
        {
            // reset all the answer window highlighting
            ResetAnswerWindowHighlighting();

            // reset the grade display
            _gameplayData.Question = null;
            UpdateLevelNameAndQuestionInfoDisplay();

            // hide the question/answer texts
            ShowQuestionAnswerTexts(false);
            ClearCountdownDisplay();

            // reset the freeze status of the players
            UnFreezePlayer(0);
            UnFreezePlayer(1);

            // set spotlights back to normal
            _spotlights[0].DoIdleAnim();
            _spotlights[1].DoIdleAnim();

            // if the game should end now then end it
            if (GameEndedConditionMet())
            {
                _canShowPauseMenu = false;
                DoGameEnded();
            }
            // else do another question
            else
            {
                // get the question content for the *next* question (this will give a chance for clients to recieve the question content before we display it on the host machine - thus mitigating host advantage)
                GenerateQuestion();

                // move to the next question ('wait for content' is a stepping stone that allows LIVE multiplayer to pick up the question content via sending/recieving the data across the network)
                _state = EnumGameplayState.WaitingForQuestionContent;
            }
        }

        /// <summary>
        /// This gameplay state is specifically to facilitate the fact that clients in a LIVE multiplayer
        /// game will be recieving the question content from the host and this could be delayed if there
        /// is lag.  So we MUST make sure that the client has new question content before continuing or
        /// they will be presented with the same question again.  Having this as a state works better than
        /// having various async tasks that would otherwise be required to handle this.
        /// </summary>
        protected virtual void ProcessWaitingForQuestionContent()
        {
            // if not showing the pause menu then move to the next question, otherwise wait until pause menu no longer showing
            // Also check if gamepads connected - otherwise wait until reconnected.
#if XBOX
            if (!_isShowingPauseMenu && AllGamepadsAreConnected())
            {
                _state = EnumGameplayState.QuestionIntro;
            }
#else
            if (!_isShowingPauseMenu)
            {
                _state = EnumGameplayState.QuestionIntro;
            }
#endif
        }

        protected virtual bool AllGamepadsAreConnected()
        {
            return GamePad.GetState(Game.Instance.XNAPlayerIndex).IsConnected;
        }

        protected virtual void ProcessGameLost()
        {
        }

        protected virtual void ProcessGameWon()
        {
        }

        protected virtual void ProcessOutro()
        {
        }

        protected void RegisterWaiting(String id)
        {
            // we don't have any tasks that cause the gameplay to wait, which can have multiple instances going on at the same time
            Assert.Fatal(!_waitList.Contains(id), "Duplicate task added to gameplay wait list");

            _waitList.Add(id);
        }

        protected void UnRegisterWaiting(String id)
        {
            _waitList.Remove(id);
        }

        protected bool IsRegisteredWaiting(String id)
        {
            return _waitList.Contains(id);
        }

        /// <summary>
        /// It is often useful to know what answer window index a button maps to as using indexes simplifies
        /// coding various stuff.
        /// </summary>
        public static int MapGamepadButtonToAnswerWindowIndex(Player.EnumGamepadButton button)
        {
            switch (button)
            {
                case Player.EnumGamepadButton.X:
                    return 0;
                    //break;

                case Player.EnumGamepadButton.Y:
                    return 1;
                    //break;

                case Player.EnumGamepadButton.A:
                    return 2;
                    //break;

                case Player.EnumGamepadButton.B:
                    return 3;
                    //break;

                default:
                    Assert.Fatal(false, "Unrecognised button: " + button);
                    return -1;
                    //break;
            }
        }

        public static Player.EnumGamepadButton MapAnswerWindowIndexToGamepadButton(int answerIndex)
        {
            switch (answerIndex)
            {
                case 0:
                    return Player.EnumGamepadButton.X;
                //break;

                case 1:
                    return Player.EnumGamepadButton.Y;
                //break;

                case 2:
                    return Player.EnumGamepadButton.A;
                //break;

                case 3:
                    return Player.EnumGamepadButton.B;
                //break;

                default:
                    Assert.Fatal(false, "Unrecognised answer index: " + answerIndex);
                    return Player.EnumGamepadButton.None;
                //break;
            }
        }

        /// <summary>
        /// It is often useful to know what answer window letter a window index maps to as some art uses letters
        /// </summary>
        protected char MapAnswerWindowIndexToLetter(int indx)
        {
            switch (indx)
            {
                case 0:
                    return 'X';
                    //break;

                case 1:
                    return 'Y';
                    //break;

                case 2:
                    return 'A';
                    //break;

                case 3:
                    return 'B';
                    //break;

                default:
                    Assert.Fatal(false, "Unrecognised answer window index: " + indx);
                    return '-';
                    //break;
            }
        }

        /// <summary>
        /// Will cause non-async gamestate processing to wait until
        /// the specified number of seconds is up.  This can be used to delay a gameplay state
        /// change for example.
        /// </summary>
        protected IEnumerator<AsyncTaskStatus> AsyncTask_Wait(float seconds)
        {
            // block non-async gameplay state processing
            RegisterWaiting("simplewait");

            // wait for timeout
            float elapsed = 0.0f;

            while (elapsed < seconds)
            {
                elapsed += _dt;
                yield return null;
            }

            // finished
            UnRegisterWaiting("simplewait");
        }

        //protected IEnumerator<AsyncTaskStatus> AsyncTask_WaitAndResetPlayerInput(float seconds)
        //{
        //    // block non-async gameplay state processing
        //    RegisterWaiting("waitandresetplayerinput");

        //    // wait for timeout
        //    float elapsed = 0.0f;

        //    while (elapsed < seconds)
        //    {
        //        elapsed += _dt;
        //        yield return null;
        //    }

        //    // reset players' input status
        //    List<Player> players = Game.Instance.ActiveGameplaySettings.Players;

        //    for (int i = 0; i < players.Count; i++)
        //    {
        //        players[i].ResetButtonPressed();
        //    }

        //    _gameplayData.ResetPlayerInput();

        //    //// unpause timer sfx
        //    //if (_isPlayingTimerSFX)
        //    //{
        //    //    MFSoundManager.Instance.ResumeSFX(MFSoundManager.EnumSFX.QuestionTimer);
        //    //}

        //    // finished
        //    UnRegisterWaiting("waitandresetplayerinput");
        //}

        protected bool AllPlayersAnswered()
        {
            for (int i = 0; i < Game.Instance.ActiveGameplaySettings.Players.Count; i++)
            {
                // if there's a player left who can answer then we can return already
                if (Game.Instance.ActiveGameplaySettings.Players[i].CanAnswer) return false;
            }

            // no players left who can answer
            return true;
        }

        protected void ResetAnswerWindowHighlighting()
        {
            for (int i = 0; i < 4; i++)
            {
                TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("hud_answerwindow_" + i).Material = _normalAnswerWindowMaterials[i];
            }
        }

        protected virtual void ShowQuestionAnswerTexts(bool show)
        {
            _questionDisplay.Visible = show;
            _answerDisplays[0].Visible = show;
            _answerDisplays[1].Visible = show;
            _answerDisplays[2].Visible = show;
            _answerDisplays[3].Visible = show;

            // TESTING - for showing what button to press for the correct answer!
//            T2DStaticSprite sprite = TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("hud_debug_correctbutton");

            if (show)
            {
//                sprite.Material = _gamepadButtonMaterials[_gameplayData.Question.RightAnswer];
                _hintshadow.Components.FindComponent<MathMultiTextComponent>().TextValue = "Hint: @newline{25} " + _gameplayData.Question.Hint;
            }
            else
            {
//                sprite.Visible = false;
                _hintshadow.Visible = false;
            }
            // TESTING ENDS
        }

        private void _OnShowPauseMenu(float val)
        {
            if (_isExiting) return;

            if (!_isShowingPauseMenu && _canShowPauseMenu && val > 0.0f)
            {
                Debug.WriteLine("Pushing in-game pause menu");

                TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("hud_shadowpanel").Visible = true;

                _isShowingPauseMenu = true;
                GameStateManager.Instance.PushOverlay(GameStateNames.PAUSEMENU, GameStateManager.Instance.GetNameOf(this));
            }
        }

        protected virtual void GenerateQuestion()
        {
            _gameplayData.Question = Game.Instance.ActiveGameplaySettings.Dealer.GetQuestion();
        }

        protected bool IsAnAnswerButton(Player.EnumGamepadButton button)
        {
            switch (button)
            {
                case Player.EnumGamepadButton.A:
                    return true;

                case Player.EnumGamepadButton.B:
                    return true;

                case Player.EnumGamepadButton.X:
                    return true;

                case Player.EnumGamepadButton.Y:
                    return true;

                default:
                    return false;
            }
        }

        protected void InitializeCountdown()
        {
            _countdownRemaining = _gameplayData.Question.TimeAllowed;
            UpdateCountdownDisplayAndSFX();
        }

        protected void ProcessCountdown()
        {
            _countdownRemaining -= _dt;

            if (_countdownRemaining <= 0.0f)
            {
                _countdownRemaining = 0.0f;
                _state = EnumGameplayState.AnswerNotGiven;
            }
            else if (_canPlayTimeRunningOutAnim && _countdownRemaining <= 3.0f)
            {
                _canPlayTimeRunningOutAnim = false;

                if (!_tutorSprites[0].IsAnimationPlaying)
                {
                    AddAsyncTask(TutorManager.Instance.PlayAnim(0, Game.Instance.ActiveGameplaySettings.Players[0].Character, TutorManager.Tutor.EnumAnim.TimeRunningOut, _tutorSprites[0]), true);
                    TutorManager.Instance.PlayTimeRunningOutSFX(0, Game.Instance.ActiveGameplaySettings.Players[0].Character);
                }

                if (!_tutorSprites[1].IsAnimationPlaying)
                {
                    AddAsyncTask(TutorManager.Instance.PlayAnim(1, Game.Instance.ActiveGameplaySettings.Players[1].Character, TutorManager.Tutor.EnumAnim.TimeRunningOut, _tutorSprites[1]), true);
                    TutorManager.Instance.PlayTimeRunningOutSFX(1, Game.Instance.ActiveGameplaySettings.Players[1].Character);
                }
            }

            bool wasInMultiplierZone = _isInMultiplierZone;

            UpdateCountdownDisplayAndSFX();

            // if countdown multiplier 'zone' status changed then play the tutor's impatient anim
            // (zone will only change once so anim will only play once)
            if (wasInMultiplierZone != _isInMultiplierZone)
            {
                if (!_tutorSprites[0].IsAnimationPlaying && !_isFrozen[0])
                {
                    AddAsyncTask(TutorManager.Instance.PlayAnim(0, Game.Instance.ActiveGameplaySettings.Players[0].Character, TutorManager.Tutor.EnumAnim.Impatient, _tutorSprites[0]), true);
                }

                if (!_tutorSprites[1].IsAnimationPlaying && !_isFrozen[1])
                {
                    AddAsyncTask(TutorManager.Instance.PlayAnim(1, Game.Instance.ActiveGameplaySettings.Players[1].Character, TutorManager.Tutor.EnumAnim.Impatient, _tutorSprites[1]), true);
                }

                UnFreezePlayer(0);
                UnFreezePlayer(1);
            }

            // reset the multiplier if necessary - doesn't matter if they get it right; they don't get their multiplier if not answering in the green!
            if (_gameplayData.CountDownRemaining_ToUse < _gameplayData.Question.TimeAllowed * 0.5f)    // using timestamp from gamedata so client and host will both use the same timer value when deciding on multipliers (otherwise lag could lead to a difference between client/host regarding multipliers)
            {
                ResetMultiplier();
                //UpdateMultiplier();
            }
        }

        protected void UpdateCountdownDisplayAndSFX()
        {
            // update the timer digits
            int countdownInteger = (int)System.Math.Ceiling(_countdownRemaining);

            string timer = countdownInteger.ToString();
            
            if (timer.Length < 2)
            {
                timer = "0" + timer;
            }

            MathMultiTextComponent text = TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("hud_timerdigits").Components.FindComponent<MathMultiTextComponent>();
            string oldTime = text.TextValue;
            text.TextValue = timer;

            if (_countdownRemaining < _gameplayData.Question.TimeAllowed * 0.5f)
            {
                _isInMultiplierZone = false;
            }
            else
            {
                _isInMultiplierZone = true;
            }

            // update the timer's outer ring
            _timerReveal.SweepAngle = 360.0f * ((_gameplayData.Question.TimeAllowed - _countdownRemaining) / _gameplayData.Question.TimeAllowed);

            // FX and SFX???
            if (oldTime != text.TextValue)
            {
                // pulse the timer digits?  (timer is showing 03 or less and the timer display has changed)
                if (countdownInteger <= 3)
                {
                    AddAsyncTask(AsyncTask_DoPulseFX(text.Owner as T2DSceneObject, 1.5f, 0.15f, 0.20f), true);
                }

                //// play sfx?
                //if (countdownInteger == 1)
                //{
                //    MFSoundManager.Instance.PlaySFXImmediately(MFSoundManager.EnumSFX.TimerPart3);
                //}
                //else if (countdownInteger == 3)
                if (countdownInteger == 3)
                {
                    MFSoundManager.Instance.PlaySFXImmediately(MFSoundManager.EnumSFX.TimerPart2);
                }
                else if (countdownInteger <= (int)(_gameplayData.Question.TimeAllowed * 0.5f) && countdownInteger > 3)
                {
                    MFSoundManager.Instance.PlaySFXImmediately(MFSoundManager.EnumSFX.TimerPart1);
                }
            }

            //// for getting a screen shot of the clock at 00 (leave this commented out normally!)
            //text.TextValue = "00";
            //_timerReveal.SweepAngle = 360.0f;
        }

        protected void ClearCountdownDisplay()
        {
            TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("hud_timerdigits").Components.FindComponent<MathMultiTextComponent>().TextValue = "00";
            _timerReveal.SweepAngle = 0.0f;
        }

        /// <summary>
        /// Returns true if the condition for ending the game has been met.
        /// The game-end condition will vary for different game modes; a game may
        /// end only when a player wins or also when a players loses depending on game type.
        /// What counts as win/lose will also vary for different gamemodes - ranging
        /// from number of questions correct, to reaching a score target, to a player quitting
        /// a multiplayer game earlier.
        /// </summary>
        protected abstract bool GameEndedConditionMet();

        /// <summary>
        /// Actually does the game ending stuff
        /// </summary>
        protected abstract void DoGameEnded();

        protected virtual void DoNormalAttack(int player, float power)
        {
            // trigger async task (must pass 'true' so it deals damage immediately)
            AddAsyncTask(AsyncTask_DoNormalAttack(player, power), true);

            // update multiplier (must be updated *after* a combo as that's the game logic requested)
            UpdateMultiplier();
        }

        protected virtual void DoDoubleNormalAttack()
        {
            // multiplier is lost - update the multplier to take effect right away in this instance
            ResetMultiplier();
            //UpdateMultiplier();

            //// trigger async task (must pass 'true' so it deals damage immediately)
            //AddAsyncTask(AsyncTask_DoDamageWithoutAttack(0), true);
            //AddAsyncTask(AsyncTask_DoDamageWithoutAttack(1), true);
            AddAsyncTask(AsyncTask_DoDamageFXWithoutDamage(0), true);   // NOTE: design change request: we no longer deal damage when both players get it wrong
            AddAsyncTask(AsyncTask_DoDamageFXWithoutDamage(1), true);
        }

        protected virtual void DoSuperAttack(int player, float power)
        {
            // trigger async task (must pass 'true' so it deals damage immediately)
            AddAsyncTask(AsyncTask_DoSuperAttack(player, power), true);

            // reset multiplier (must be reset *after* a using the super obviously so that the combo multiplier will apply to the super)
            ResetMultiplier();
        }

        protected virtual void DealDamage(int player, int damage)
        {
            _health[player] -= damage;

            if (_health[player] <= 0)
            {
                _health[player] = 0;
            }

            // player damaged AI then update player score
            if (player == 1 && _playerScoreText.Visible)
            {
                AddAsyncTask(AsyncTask_UpdateScore((int)((float)damage * Game.Instance.ActiveGameplaySettings.ScoreMultiplier)), true);
            }
        }

        private void UpdatePlayerScoreDisplay()
        {
            _playerScoreText.Components.FindComponent<MathMultiTextComponent>().TextValue = (Game.Instance.ActiveGameplaySettings.PlayerScore + _playerScore).ToString();
        }

        /// <summary>
        /// Does the fx for updating the score display
        /// </summary>
        protected IEnumerator<AsyncTaskStatus> AsyncTask_UpdateScore(int pointsToAward)
        {
            // pause for a moment - other animations running that we don't want to clash with (right/wrong text)
            float elapsed = 0.0f;

            while (elapsed < 0.25f)
            {
                elapsed += _dt;
                yield return null;
            }

            // update the score display by counting up the score X points at a time - calculated to always take no longer than 1 second
            int pointsLeftToAward = pointsToAward;
            const float DELAY = 0.05f;
            int scoreInc = (int)((float)pointsLeftToAward / (1.0f / DELAY));

            if (scoreInc < 1) scoreInc = 1;   // silliness check

            float delayLeft = DELAY; // causes a delay between each score increment happening (or it would happen to fast to see properly)

            while (pointsLeftToAward > 0)
            {
                if (delayLeft > 0.0f)
                {
                    delayLeft -= _dt;
                }
                else
                {
                    delayLeft += DELAY;

                    if (pointsLeftToAward > scoreInc)                    
                    {
                        _playerScore += scoreInc;
                        pointsLeftToAward -= scoreInc;
                    }
                    else
                    {
                        _playerScore += pointsLeftToAward;
                        pointsLeftToAward = 0;
                    }

                    UpdatePlayerScoreDisplay();
                }

                yield return null;
            }
        }

        /// <summary>
        /// Does a pulse fx for the specified scene object
        /// </summary>
        protected IEnumerator<AsyncTaskStatus> AsyncTask_DoPulseFX(T2DSceneObject target, float sizeMultiplier, float pulseTimeExpand, float pulseTimeShrink)
        {
            // store original size so we can return the object to it's correct size later
            Vector2 originalSize = target.Size;

            // expand the object
            float mul;
            float elapsed = 0.0f;

            while (elapsed < pulseTimeExpand)
            {
                elapsed += _dt;
                mul = MathHelper.Lerp(1.0f, sizeMultiplier, elapsed / pulseTimeExpand);
                target.Size = originalSize * mul;
                yield return null;
            }

            // shrink the object
            elapsed = 0.0f;

            while (elapsed < pulseTimeShrink)
            {
                elapsed += _dt;
                mul = MathHelper.Lerp(sizeMultiplier, 1.0f, elapsed / pulseTimeShrink);
                target.Size = originalSize * mul;
                yield return null;
            }

            // make sure the object ends up at its original size
            target.Size = originalSize;
        }

        /// <summary>
        /// Shows the 'get ready' message at the start of the game.
        /// </summary>
        protected IEnumerator<AsyncTaskStatus> AsyncTask_ShowMessage_GetReady()
        {
            // block non-async gameplay state processing
            RegisterWaiting("showgetready");

            // wait for just a moment before showing the message
            float elapsed = 0.0f;

            while (elapsed < 0.5f)
            {
                elapsed += _dt;
                yield return null;
            }

            T2DAnimatedSprite anim = TorqueObjectDatabase.Instance.FindObject<T2DAnimatedSprite>("hud_message_GETREADY");
            anim.Visible = true;
            anim.Position = Vector2.Zero;

            // play sfx
            MFSoundManager.Instance.PlaySFX(MFSoundManager.EnumSFX.GetReady);

            // play the animation forwards - animates onto the screen
            anim.PlayBackwards = false;
            anim.PlayAnimation();

            // display the message for a while
            elapsed = 0.0f;

            while (elapsed < 1.5f)
            {
                elapsed += _dt;
                yield return null;
            }

            // play the animation backwards - animates off the screen
            anim.PlayBackwards = true;
            anim.ResumeAnimation();

            // wait for the anim to finish and then hide it
            while (anim.IsAnimationPlaying)
            {
                yield return null;
            }

            anim.Visible = false;

            //// pause a moment before moving along with the rest of the game processing
            //elapsed = 0.0f;

            //while (elapsed < 0.5f)
            //{
            //    elapsed += _dt;
            //    yield return null;
            //}

            // and finally - we will 'generate' the first question - we do this here so that client won't lag behind host (i.e. client won't send the 'ready for next question' msg until it's actually finished displaying the intro)
            GenerateQuestion();

            // finished
            UnRegisterWaiting("showgetready");
        }

        /// <summary>
        /// Shows the 'get ready' message at the start of the game.
        /// </summary>
        protected IEnumerator<AsyncTaskStatus> AsyncTask_ShowMessage_NextQuestion(int questionNumber)
        {
            // block non-async gameplay state processing
            RegisterWaiting("shownextquestion");

            // wait for just a moment before showing the message
            float elapsed = 0.0f;

            while (elapsed < 0.5f)
            {
                elapsed += _dt;
                yield return null;
            }

            // play the sfx
            MFSoundManager.Instance.PlaySFX(MFSoundManager.EnumSFX.QuestionAppears);

            // setup and display the message object
            T2DSceneObject messageObj = TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("hud_messagetext");
            MathMultiTextComponent messageText = messageObj.Components.FindComponent<MathMultiTextComponent>();
            messageObj.Visible = true;
            messageObj.Position = Vector2.Zero;
            messageText.Font = Game.EnumMathFreakFont.Default;
            messageText.TextValue = "Question " + questionNumber;

            // now setup the question type message that appears beneath the 'question xxxx' message
            T2DSceneObject questionType = TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("hud_levelname");
            MathMultiTextComponent questionTypeText = questionType.Components.FindComponent<MathMultiTextComponent>();
            questionType.Visible = true;
            questionTypeText.Font = Game.EnumMathFreakFont.Default;
            questionTypeText.TextValue = _gameplayData.Question.LevelName.ToUpper();

            // yield for one tick to allow the text to update and then position the message so it's under the "Question" message
            yield return null;
            questionType.Position = new Vector2(messageObj.Position.X, messageObj.Position.Y + (messageObj.Size.Y * 0.5f) + (questionType.Size.Y * 0.5f) - 20.0f);

            // show the grade
            UpdateLevelNameAndQuestionInfoDisplay();

            // wait for a while before hiding the message and continuing the gameplay processing
            elapsed = 0.0f;

            while (elapsed < 1.0f)
            {
                elapsed += _dt;
                yield return null;
            }

            // hide the message
            messageObj.Visible = false;
            questionType.Visible = false;

            //// wait for just a moment before continuing gameplay processing
            //elapsed = 0.0f;

            //while (elapsed < 0.5f)
            //{
            //    elapsed += _dt;
            //    yield return null;
            //}

            // finished
            UnRegisterWaiting("shownextquestion");
        }

        protected int OtherPlayer(int playerNum)
        {
            return playerNum == 0 ? 1 : 0;
        }

        private void DoFX_AnswerCorrect(int playerNum)
        {
            //PlayTutorAnimCorrect(playerNum);
            //PlayTutorAnimWrong(OtherPlayer(playerNum));
            _spotlights[_activePlayer].DoRightWrongAnim(Color.Green);
            AddAsyncTask(AsyncTask_ShowRightWrongMessage(_activePlayer, true), true);

            // sfx
            MFSoundManager.Instance.PlaySFX(MFSoundManager.EnumSFX.Correct);
        }

        private void DoFX_AnswerWrong(int playerNum)
        {
            MFSoundManager.Instance.PlaySFX(MFSoundManager.EnumSFX.WrongBuzzard);
            _spotlights[_activePlayer].DoRightWrongAnim(Color.Red);
            AddAsyncTask(AsyncTask_ShowRightWrongMessage(_activePlayer, false), true);
        }

        private void DoFX_AnswerNotGiven()
        {
            MFSoundManager.Instance.PlaySFX(MFSoundManager.EnumSFX.WrongBuzzard);
            AddAsyncTask(AsyncTask_DoClockExplosion(), true);
            //PlayTutorAnimWrong(0);
            //PlayTutorAnimWrong(1);
        }

        protected IEnumerator<AsyncTaskStatus> AsyncTask_ShowRightWrongMessage(int player, bool correct)
        {
            // get the message object that we need and set it up
            T2DStaticSprite msg = TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("hud_rightwrongmessage" + player);
            SimpleMaterial material = (correct ? _correctMaterial : _wrongMaterial) as SimpleMaterial;
            msg.Material = material;
            Texture2D texture = material.Texture.Instance as Texture2D;
            msg.Size = new Vector2((float)texture.Width * ((float)msg.Size.Y / texture.Height), msg.Size.Y);  // adjust the width of the sprite so it scales down the texture while preserving the texture's aspect ratio

            // initialize the msg ready for the animation
            Vector2 baseSize = msg.Size;
            msg.Size = new Vector2(1, 1);
            msg.Visible = true;

            // do a grow/shrink text effect to magic the msg onto the screen
            // ...grow
            float targetSizeY = baseSize.Y * 1.25f;
            float mul = 1.00f;

            while (msg.Size.Y < targetSizeY)
            {
                mul *= 1.02f;
                msg.Size = baseSize * mul;
                yield return null;
            }

            // ...shrink
            targetSizeY = baseSize.Y;

            while (msg.Size.Y > targetSizeY)
            {
                mul *= 0.99f;
                msg.Size = baseSize * mul;
                yield return null;
            }

            msg.Size = baseSize;    // makes sure it ends up the right size at the end of the effect

            // pause for a moment to let the player see the message
            float elapsed = 0.0f;

            while (elapsed < 2.0f)
            {
                elapsed += _dt;
                yield return null;
            }

            // hide the message
            msg.Visible = false;
        }

        protected virtual void UpdateMultiplier()
        {
            //if (_isInMultiplierZone)
            if (_gameplayData.CountDownRemaining_ToUse >= _gameplayData.Question.TimeAllowed * 0.5f)    // using timestamp from gamedata so client and host will both use the same timer value when deciding on multipliers (otherwise lag could lead to a difference between client/host regarding multipliers)
            {
                _multiplier += MULTIPLIER_INC;

                if (_multiplier > MULTIPLIER_MAX)
                {
                    _multiplier = MULTIPLIER_MAX;
                }
            }
            else
            {
                ResetMultiplier();
            }
        }

        protected virtual void ResetMultiplier()
        {
            // reset multiplier
            _multiplier = 1.0f;

            // do the end of sequence message stuff
            //DoEndOfSequenceFX(_sequenceCount, _sequenceScore);

            // reset the sequence data
            _sequenceCount = 0;
        }

        // overriding this allows the mutliplayer game to do decide
        // whether or not to actually reset the multiplier after a wrong answer.
        // required because in the multiplayer game the other person still has a go
        // so we shouldn't nuke *their* multiplier just because the *other* player
        // got the answer wrong.
        protected virtual void ResetMultiplierAfterWrongAnswer()
        {
            ResetMultiplier();
        }

        public static int CalcNormalDamage(float power)
        {
            return (int)BASICDAMAGE;
        }

        public static int CalcSuperDamage(float power)
        {
            return (int)(power * SUPERDAMAGE);
        }

        protected virtual void DoEndOfSequenceFX(int sequenceCount, int sequenceScore)
        {
            if (sequenceCount > 2)
            {
                AddAsyncTask(AsyncTask_ShowEndOfSequenceMessage(sequenceCount, _activePlayer), true);
                AddAsyncTask(AsyncTask_ShowEndOfSequenceStats(sequenceCount, sequenceScore, _activePlayer), true);
            }
        }

        protected virtual void DoFX_Combo(int player, int answer, float comboCounter)
        {
            AddAsyncTask(AsyncTask_DisplayCombo(player, answer, comboCounter), true);
        }

        protected IEnumerator<AsyncTaskStatus> AsyncTask_ShowEndOfSequenceMessage(int sequenceCount, int player)
        {
            string msg;

            if (sequenceCount < 5)
            {
                msg = "Not Bad!";
            }
            else if (sequenceCount < 10)
            {
                msg = "Awesome!";
            }
            else
            {
                msg = "LUCKY!";
            }

            // setup and display the message object
            // TODO: show the proper art when we have it
            T2DStaticSprite floater = TorqueObjectDatabase.Instance.CloneObject<T2DStaticSprite>("hud_sequencemessagetext");
            MathMultiTextComponent text = floater.Components.FindComponent<MathMultiTextComponent>();
            text.TextValue = msg;
            Assert.Fatal(floater.Folder == null, "floater object is already in a Folder");
            floater.Folder = _sceneFolder;   // must create this object in our folder and not any overlayed scene's folder or when that scene is unloaded so is this object!
            TorqueObjectDatabase.Instance.Register(floater);

            yield return null;  // give the text component time to create it's texture

            // get the floater ready to float
            Vector2 baseSize = floater.Size;
            floater.Position = TorqueObjectDatabase.Instance.CloneObject<T2DStaticSprite>("hud_sequencefxpos" + player).Position;
            floater.Visible = true;
            floater.VisibilityLevel = 1.0f;

            SimpleMaterial material = (floater.Material as SimpleMaterial);
            material.IsColorBlended = true;
            material.IsTranslucent = true;

            // floater floats upwards whilst growing and fading out - anim ends when floater transparency reaches a low enough alpha level
            Vector2 posInc = new Vector2(0.0f, -0.25f);

            while (floater.VisibilityLevel >= 0.1f)
            {
                floater.VisibilityLevel -= 0.0025f;
                floater.Size *= 1.0005f;
                floater.Position += posInc;
                yield return null;
            }

            floater.MarkForDelete = true;   // we're done with the floater now
        }

        protected IEnumerator<AsyncTaskStatus> AsyncTask_ShowEndOfSequenceStats(int sequenceCount, int sequenceScore, int player)
        {
            //// wait for just a moment before doing the fx (we are following the 'sequence message fx' so give it a fraction just to get on screen and start animating
            //float elapsed = 0.0f;

            //while (elapsed < 0.10f)
            //{
            //    elapsed += _dt;
            //    yield return null;
            //}

            // setup and display the message object
            T2DStaticSprite floater = TorqueObjectDatabase.Instance.CloneObject<T2DStaticSprite>("hud_sequencestatstext");
            MathMultiTextComponent text = floater.Components.FindComponent<MathMultiTextComponent>();
            text.TextValue = sequenceCount.ToString() + "&nbspIN&nbspA&nbspROW @newline{} " + sequenceScore + "&nbsppts!";
            Assert.Fatal(floater.Folder == null, "floater object is already in a Folder");
            floater.Folder = _sceneFolder;   // must create this object in our folder and not any overlayed scene's folder or when that scene is unloaded so is this object!
            TorqueObjectDatabase.Instance.Register(floater);

            yield return null;  // give the text component time to create it's texture

            // get the floater ready to float
            Vector2 baseSize = floater.Size;
            floater.Position = TorqueObjectDatabase.Instance.CloneObject<T2DStaticSprite>("hud_sequencefxpos" + player).Position + new Vector2(0.0f, 60.0f);
            floater.Visible = true;
            floater.VisibilityLevel = 1.0f;

            SimpleMaterial material = (floater.Material as SimpleMaterial);
            material.IsColorBlended = true;
            material.IsTranslucent = true;

            // floater floats upwards whilst growing and fading out - anim ends when floater transparency reaches a low enough alpha level
            Vector2 posInc = new Vector2(0.0f, -0.25f);

            while (floater.VisibilityLevel >= 0.1f)
            {
                floater.VisibilityLevel -= 0.0025f;
                floater.Size *= 1.0005f;
                floater.Position += posInc;
                yield return null;
            }

            floater.MarkForDelete = true;   // we're done with the floater now
        }

        protected IEnumerator<AsyncTaskStatus> AsyncTask_DoNormalAttack(int player, float power)
        {
            RegisterWaiting("donormalattack");

            // deal damage - do this immediately so that it will definitely count when the game checks for end conditions no matter how soon that happens (right now it's delayed 2 seconds, but let's be paranoid about possible future changes :-)
            int damage = CalcNormalDamage(power);
            DealDamage(OtherPlayer(player), damage);

            // play attack animation for attacker
            T2DAnimatedSprite damageSprite = TorqueObjectDatabase.Instance.FindObject<T2DAnimatedSprite>("hud_superfxanim" + OtherPlayer(player));
            TutorManager.TutorAttackAnimStatus animStatus = new TutorManager.TutorAttackAnimStatus();
            AddAsyncTask(TutorManager.Instance.PlayNormalAttackAnim(player, Game.Instance.ActiveGameplaySettings.Players[player].Character, _tutorSprites[player], damageSprite, animStatus), false);

            // wait until the animation reaches the damage dealing point
            while (animStatus.AnimStatus != TutorManager.TutorAttackAnimStatus.EnumAnimStatus.ReachedDamageDealingFrame) yield return null;

            // play the other player's damage animation
            AddAsyncTask(TutorManager.Instance.PlayAnim(OtherPlayer(player), Game.Instance.ActiveGameplaySettings.Players[OtherPlayer(player)].Character, TutorManager.Tutor.EnumAnim.Damaged, _tutorSprites[OtherPlayer(player)]), false);

            // trigger the healthbar updater async task (update the visual display of health data)
            AddAsyncTask(AsyncTask_UpdateHealthBarDisplay(OtherPlayer(player), damage), false);

            // wait for opponent's damaged anim to finish
            while (!_tutorSprites[OtherPlayer(player)].IsAnimationPlaying) yield return null;
            while (_tutorSprites[OtherPlayer(player)].IsAnimationPlaying) yield return null;

            // play KO animation?
            if (_health[OtherPlayer(player)] == 0)
            {
                AddAsyncTask(TutorManager.Instance.PlayAnim(OtherPlayer(player), Game.Instance.ActiveGameplaySettings.Players[OtherPlayer(player)].Character, TutorManager.Tutor.EnumAnim.Lose, _tutorSprites[OtherPlayer(player)]), false);

                // wait for KO animation to finish
                while (!_tutorSprites[OtherPlayer(player)].IsAnimationPlaying) yield return null;
                while (_tutorSprites[OtherPlayer(player)].IsAnimationPlaying) yield return null;
            }

            // finally, make sure the attack animation has finished
            while (animStatus.AnimStatus != TutorManager.TutorAttackAnimStatus.EnumAnimStatus.Completed) yield return null;

            UnRegisterWaiting("donormalattack");
        }

        protected IEnumerator<AsyncTaskStatus> AsyncTask_DoAutoWin(int player)
        {
            RegisterWaiting("doautowin" + player);

            // silliness check - the winning player should have some health left in order to win
            if (_health[player] == 0)
            {
                _health[player] = 1;
            }

            // set the other player's health to zero - do this immediately so that it will definitely count when the game checks for end conditions no matter how soon that happens
            int damage = _health[OtherPlayer(player)];
            _health[OtherPlayer(player)] = 0;            

            // show message that a player left the game
            T2DSceneObject messageObj = TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("hud_messagetext");
            MathMultiTextComponent messageText = messageObj.Components.FindComponent<MathMultiTextComponent>();
            messageObj.Visible = true;
            messageObj.Position = Vector2.Zero;
            messageText.Font = Game.EnumMathFreakFont.Default;
            messageText.TextValue = Game.Instance.ActiveGameplaySettings.Players[OtherPlayer(player)].GamerTag + " Left";

            // hide any other message text stuff (if was displaying another message such as 'next question' for example)
            TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("hud_levelname").Visible = false;

            // play damage animation
            AddAsyncTask(TutorManager.Instance.PlayAnim(OtherPlayer(player), Game.Instance.ActiveGameplaySettings.Players[OtherPlayer(player)].Character, TutorManager.Tutor.EnumAnim.Damaged, _tutorSprites[OtherPlayer(player)]), false);

            // trigger the healthbar updater async task (update the visual display of health data)
            AddAsyncTask(AsyncTask_UpdateHealthBarDisplay(OtherPlayer(player), damage), false);

            // wait until damage animation finishes
            while (!_tutorSprites[OtherPlayer(player)].IsAnimationPlaying) yield return null;
            while (_tutorSprites[OtherPlayer(player)].IsAnimationPlaying) yield return null;

            // play KO animation
            AddAsyncTask(TutorManager.Instance.PlayAnim(OtherPlayer(player), Game.Instance.ActiveGameplaySettings.Players[OtherPlayer(player)].Character, TutorManager.Tutor.EnumAnim.Lose, _tutorSprites[OtherPlayer(player)]), false);

            // wait until KO animation finishes
            while (!_tutorSprites[OtherPlayer(player)].IsAnimationPlaying) yield return null;
            while (_tutorSprites[OtherPlayer(player)].IsAnimationPlaying) yield return null;

            // hide the message
            messageObj.Visible = false;

            // exiting was set to true to stop anything else doing stuff, but set it to false
            // so that the gamewon state can process (which will then immediately set it back to
            // true again).
            _isExiting = false;

            UnRegisterWaiting("doautowin" + player);
        }

        protected IEnumerator<AsyncTaskStatus> AsyncTask_DoDamageFXWithoutDamage(int player)
        {
            RegisterWaiting("dodamagefxwithoutdamage" + player);

            // play damage animation
            AddAsyncTask(TutorManager.Instance.PlayAnim(player, Game.Instance.ActiveGameplaySettings.Players[player].Character, TutorManager.Tutor.EnumAnim.Damaged, _tutorSprites[player]), false);

            // wait until damage animation finishes
            while (!_tutorSprites[player].IsAnimationPlaying) yield return null;
            while (_tutorSprites[player].IsAnimationPlaying) yield return null;

            UnRegisterWaiting("dodamagefxwithoutdamage" + player);
        }

        protected IEnumerator<AsyncTaskStatus> AsyncTask_DoSuperAttack(int player, float power)
        {
            RegisterWaiting("dosuperattack");

            // deal damage - do this immediately so that it will definitely count when the game checks for end conditions no matter how soon that happens (right now it's delayed 2 seconds, but let's be paranoid about possible future changes :-)
            int damage = CalcSuperDamage(power);
            DealDamage(OtherPlayer(player), damage);

            // play lightening animation
            T2DAnimatedSprite lightening = null;

            if (power >= 5.0f)
            {
                lightening = TorqueObjectDatabase.Instance.FindObject<T2DAnimatedSprite>("hud_superfxanim" + player);
                lightening.Visible = true;
                lightening.Layer = _tutorSprites[0].Layer + 1;

                if (power >= 9.0f)
                {
                    MFSoundManager.Instance.PlaySFX(MFSoundManager.EnumSFX.Lightening3);
                    lightening.PlayAnimation(TorqueObjectDatabase.Instance.FindObject<T2DAnimationData>("Lightning_Effect__Level_3Animation"));
                }
                else if (power >= 7.0f)
                {
                    MFSoundManager.Instance.PlaySFX(MFSoundManager.EnumSFX.Lightening2);
                    lightening.PlayAnimation(TorqueObjectDatabase.Instance.FindObject<T2DAnimationData>("Lightning_Effect__Level_2Animation"));
                }
                else if (power >= 5.0f)
                {
                    MFSoundManager.Instance.PlaySFX(MFSoundManager.EnumSFX.Lightening1);
                    lightening.PlayAnimation(TorqueObjectDatabase.Instance.FindObject<T2DAnimationData>("Lightning_Effect__Level_1Animation"));
                }

                // pause for a moment to admire the lightening
                float elapsedTime = 0.0f;

                while (elapsedTime < 1.0f)
                {
                    elapsedTime += _dt;
                    yield return null;
                }
            }

            // play super attack animation
            T2DAnimatedSprite damageSprite = TorqueObjectDatabase.Instance.FindObject<T2DAnimatedSprite>("hud_superfxanim" + OtherPlayer(player));
            TutorManager.TutorAttackAnimStatus animStatus = new TutorManager.TutorAttackAnimStatus();
            AddAsyncTask(TutorManager.Instance.PlaySuperAttackAnim(player, Game.Instance.ActiveGameplaySettings.Players[player].Character, _tutorSprites[player], damageSprite, _ghostSprites[player], power, TASKLIST_GAMEPLAY_ASYNC_EVENTS, animStatus), false);

            // wait until the animation reaches the damage dealing point
            while (animStatus.AnimStatus != TutorManager.TutorAttackAnimStatus.EnumAnimStatus.ReachedDamageDealingFrame) yield return null;

            // play damage animation
            AddAsyncTask(TutorManager.Instance.PlayAnim(OtherPlayer(player), Game.Instance.ActiveGameplaySettings.Players[OtherPlayer(player)].Character, TutorManager.Tutor.EnumAnim.Damaged, _tutorSprites[OtherPlayer(player)]), false);

            // trigger super KO fx if it will be a KO
            if (_health[OtherPlayer(player)] == 0)
            {
                AddAsyncTask(AsyncTask_DoSuperKO(_ghostSprites[player]), false);
            }

            // trigger the healthbar updater async task (update the visual display of health data)
            AddAsyncTask(AsyncTask_UpdateHealthBarDisplay(OtherPlayer(player), damage), false);

            // wait until the opponent's damage animation stops
            while (!_tutorSprites[OtherPlayer(player)].IsAnimationPlaying) yield return null;
            while (_tutorSprites[OtherPlayer(player)].IsAnimationPlaying) yield return null;

            // if it's a KO then play the opponent's KO anim and the KO text fx
            if (_health[OtherPlayer(player)] == 0)
            {
                AddAsyncTask(TutorManager.Instance.PlayAnim(OtherPlayer(player), Game.Instance.ActiveGameplaySettings.Players[OtherPlayer(player)].Character, TutorManager.Tutor.EnumAnim.Lose, _tutorSprites[OtherPlayer(player)]), false);
                AddAsyncTask(AsyncTask_DoSuperKOText(), false);

                // wait for the KO anim to start
                while (!_tutorSprites[OtherPlayer(player)].IsAnimationPlaying) yield return null;
            }

            // when the attack animation has finished then we are done with the lightening (if there is any)
            while (animStatus.AnimStatus != TutorManager.TutorAttackAnimStatus.EnumAnimStatus.Completed) yield return null;

            if (lightening != null)
            {
                lightening.PauseAnimation();
                lightening.Visible = false;
            }

            // wait for KO animation and KO text fx to finish?
            if (_health[OtherPlayer(player)] == 0)
            {
                while (_tutorSprites[OtherPlayer(player)].IsAnimationPlaying) yield return null;
                while (IsRegisteredWaiting("superkotext")) yield return null;
            }

            UnRegisterWaiting("dosuperattack");
        }

        protected IEnumerator<AsyncTaskStatus> AsyncTask_UpdateHealthBarDisplay(int player, int damage)
        {
            // trigger a floater to show damage dealt
            AddAsyncTask(AsyncTask_ShowDamage(player, damage), false);

            //// change the healthbar colour if health level is getting critical
            //if (_health[player] <= CRITICAL_HEALTH_LEVEL)
            //{
            //    (_healthbars[player].Owner as T2DStaticSprite).ColorTint = Color.Red;
            //}

            // reduce healthbar size (animated)
            OffsetTextureComponent healthBar = _healthbars[player];
            int damageLeftToDeal = damage;
            const float DELAY = 0.05f;
            int damageStep = (int)(DELAY * (float)damage) / 2; // how many points of damage to reduce the bar by on each 'frame' of the animation - it's worked out so it will always take no longer than 2 seconds regardless of the damage amount (i.e. just reduces faster/slower accordingly)

            if (damageStep < 1) damageStep = 1; // silliness check

            float delayLeft = DELAY; // causes a delay between each score increment happening (or it would happen to fast to see properly)

            while (damageLeftToDeal > 0)
            {
                if (delayLeft > 0.0f)
                {
                    delayLeft -= _dt;
                }
                else
                {
                    delayLeft = DELAY;

                    if (damageLeftToDeal > damageStep)
                    {
                        _healthDisplayed[player] -= damageStep;
                        damageLeftToDeal -= damageStep;
                    }
                    else
                    {
                        _healthDisplayed[player] -= damageLeftToDeal;
                        damageLeftToDeal = 0;
                    }

                    if (_healthDisplayed[player] < 0)
                    {
                        _healthDisplayed[player] = 0;
                    }

                    // note: to update the healthbar we have to convert to fractional value (the bar can represent any *actual* number of health points, but the bar will always be the same size on screen regardless, so we need to normalize the health amount in order to update the bar properly)
                    // note: also need adjust for the health bar ends being angled - i.e. the actual health bar length should be treated as shorter than the texture really is, so we will adjust for the 'base' of the healthbar.
                    float baseAdjustment = 19.0f / 336.0f;  // where 336 is the texture width and 19 is the amount the slanted edge part slants inward by
                    float displayAdjustment = (336.0f - 19.0f) / 336.0f;    // this will be use to multiply the normalized (fractional) health value to fit it into adjusted space we require
                    _healthbars[player].Offset = new Vector2(baseAdjustment + (displayAdjustment * (float)_healthDisplayed[player] / (float)Game.Instance.ActiveGameplaySettings.EnergyBar), _healthbars[player].Offset.Y);

                    // update the healthbar colour too while where at it...
                    (_healthbars[player].Owner as T2DStaticSprite).ColorTint = Color.Lerp(Color.Red, Color.Yellow, displayAdjustment * (float)_healthDisplayed[player] / (float)Game.Instance.ActiveGameplaySettings.EnergyBar);
                }

                yield return null;
            }
        }

        protected IEnumerator<AsyncTaskStatus> AsyncTask_ShowDamage(int player, int damage)
        {
            // create a floater at the appropriate location
            T2DStaticSprite floater = TorqueObjectDatabase.Instance.CloneObject<T2DStaticSprite>("hud_damagefloater" + player);
            MathMultiTextComponent text = floater.Components.FindComponent<MathMultiTextComponent>();
            text.TextValue = "DAMAGE " + damage + "pts";
            text.RGBA = new Vector4(0, 0.5f, 0, 1);
            Assert.Fatal(floater.Folder == null, "floater object is already in a Folder");
            floater.Folder = _sceneFolder;   // must create this object in our folder and not any overlayed scene's folder or when that scene is unloaded so is this object!
            TorqueObjectDatabase.Instance.Register(floater);

            yield return null;  // give the text component time to create it's texture

            // get the floater ready to float
            Vector2 baseSize = floater.Size;
            floater.Visible = true;

            SimpleMaterial material = (floater.Material as SimpleMaterial);
            material.IsColorBlended = true;
            material.IsTranslucent = true;

            // floater floats upwards whilst growing and fading out - anim ends when floater transparency reaches a low enough alpha level
            Vector2 posInc = new Vector2(0.0f, -0.5f);

            while (floater.VisibilityLevel >= 0.1f)
            {
                floater.VisibilityLevel -= 0.005f;
                floater.Size *= 1.001f;
                floater.Position += posInc;
                yield return null;
            }

            floater.MarkForDelete = true;   // we're done with the floater now
        }

        protected IEnumerator<AsyncTaskStatus> AsyncTask_DoSuperKO(T2DAnimatedSprite[] ghostSprites)
        {
            T2DAnimatedSprite overlay0 = TorqueObjectDatabase.Instance.FindObject<T2DAnimatedSprite>("hud_superfxanim0");
            T2DAnimatedSprite overlay1 = TorqueObjectDatabase.Instance.FindObject<T2DAnimatedSprite>("hud_superfxanim1");

            // bring characters, overlayed animations, and trail fx to foreground
            int originalCharacterLayer = _tutorSprites[0].Layer;
            int originalOverlayLayer0 = overlay0.Layer;
            int originalOverlayLayer1 = overlay1.Layer;

            _tutorSprites[0].Layer = 16;
            _tutorSprites[1].Layer = 16;

            if (originalOverlayLayer0 < originalCharacterLayer)
            {
                overlay0.Layer = 15;
            }
            else
            {
                overlay0.Layer = 17;
            }

            if (originalOverlayLayer1 < originalCharacterLayer)
            {
                overlay1.Layer = 15;
            }
            else
            {
                overlay1.Layer = 17;
            }

            for (int i = 0; i < SUPERTRAILFX_NUMGHOSTS; i++)
            {
                ghostSprites[i].Layer = _tutorSprites[0].Layer + 1;
            }

            // trigger the KO sfx
            MFSoundManager.Instance.PlaySFX(MFSoundManager.EnumSFX.KO);

            // make the KO BG visible
            T2DSceneObject bg = TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("hud_superkobg");
            bg.Visible = true;
            bg.Layer = 19;

            // explode a nice light ray/explosion effect onto the screen
            // ...setup
            T2DStaticSprite rays = TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("hud_superkorays");

            float targetWidth = rays.Size.X;

            rays.Layer = 18;
            rays.Visible = true;
            rays.Size = new Vector2(0.0f, 0.0f);
            rays.Position = Vector2.Zero;

            yield return null;

            // ...explode the rays
            float scaleWidth = 0.1f;

            while (scaleWidth <= 1.0f)
            {
                rays.Size = new Vector2(targetWidth * scaleWidth);
                scaleWidth *= 1.10f;
                yield return null;
            }

            // wait for the super attack to finish and then return everything to how it was before
            while (IsRegisteredWaiting("dosuperattack")) yield return null;

            bg.Visible = false;
            rays.Visible = false;
            _tutorSprites[0].Layer = originalCharacterLayer;
            _tutorSprites[1].Layer = originalCharacterLayer;
            overlay0.Layer = originalOverlayLayer0;
            overlay1.Layer = originalOverlayLayer1;

            for (int i = 0; i < SUPERTRAILFX_NUMGHOSTS; i++)
            {
                ghostSprites[i].Layer = _tutorSprites[0].Layer + 1;
            }
        }

        protected IEnumerator<AsyncTaskStatus> AsyncTask_DoSuperKOText()
        {
            RegisterWaiting("superkotext");

            T2DStaticSprite text = TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("hud_superkotext");

            // initialize the text's size and position
            Vector2 baseSize = text.Size;
            text.Position = Vector2.Zero;
            text.Visible = true;

            // 'stamp' the text onto the screen - i.e. text starts out massive and speedily shrinks as if it is crashing into rest of the art
            float mul = 4.00f;

            do
            {
                mul *= 0.95f;
                text.Size = baseSize * mul;
                yield return null;

            } while (text.Size.Y > baseSize.Y);

            text.Size = baseSize;    // makes sure it ends up the right size at the end of the effect

            // pause for a moment to make sure the players get to see the text
            float elapsedTime = 0.0f;

            while (elapsedTime < 2.5f)
            {
                elapsedTime += _dt;
                yield return null;
            }

            // done with the text now
            text.Visible = false;

            UnRegisterWaiting("superkotext");
        }

        protected IEnumerator<AsyncTaskStatus> AsyncTask_DisplayCombo(int player, int answer, float comboCounter)
        {
            // not decided on an attack yet
            _attackDecision = EnumAttackDecision.None;

            // pause for a moment - other animations running that we don't want to clash with (right/wrong text)
            float elapsed = 0.0f;

            while (elapsed < 0.25f)
            {
                elapsed += _dt;
                yield return null;
            }

            // create a text floater at the scoring answer's location
            T2DStaticSprite floater = TorqueObjectDatabase.Instance.CloneObject<T2DStaticSprite>("hud_scorefloater");
            floater.Components.FindComponent<MathMultiTextComponent>().TextValue = comboCounter.ToString() + "x";
            Assert.Fatal(floater.Folder == null, "floater object is already in a Folder");
            floater.Folder = _sceneFolder;   // must create this object in our folder and not any overlayed scene's folder or when that scene is unloaded so is this object!
            TorqueObjectDatabase.Instance.Register(floater);

            yield return null;  // give the text component time to create it's texture

            // initialize the floater's size and position
            Vector2 baseSize = floater.Size;
            floater.Position = new Vector2(_answerDisplays[answer].Position.X, _answerDisplays[answer].Position.Y - 50.0f);
            floater.Size = new Vector2(1, 1);
            floater.Visible = true;

            SimpleMaterial material = (floater.Material as SimpleMaterial);
            material.IsColorBlended = true;
            material.IsTranslucent = true;

            // do a grow/shrink text effect to magic the floater onto the screen
            float targetSizeY = baseSize.Y * 1.5f;
            float mul = 1.00f;

            while (floater.Size.Y < targetSizeY)
            {
                mul *= 1.01f;
                floater.Size = baseSize * mul;
                yield return null;
            }

            targetSizeY = baseSize.Y;

            while (floater.Size.Y > targetSizeY)
            {
                mul *= 0.99f;
                floater.Size = baseSize * mul;
                yield return null;
            }

            floater.Size = baseSize;    // makes sure it ends up the right size at the end of the effect

            //// create one or more ghosts
            //AddAsyncTask(AsyncTask_DisplayComboGhost(answer, comboCounter, 2.5f, 1.04f), false);

            // show the prompt to use the super attack if minimum required power level has been reached
            if (comboCounter > 2)
            {
                T2DStaticSprite superprompt = TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("hud_superattackprompt" + player);
                superprompt.Visible = true;
            }

            // pause for a moment (and also poll player input and decide on attack to do)
            elapsed = 0.0f;
            float delay = comboCounter > 2 ? 2.0f : 1.0f;

            while (elapsed < delay)
            {
                elapsed += _dt;

                // poll the player's input if it's possible for them to use their super attack
                if (comboCounter > 2 && _gameplayData.SuperAttackPressed[player])
                {
                    _attackDecision = EnumAttackDecision.Super;

                    // done with showing the combo now...
                    elapsed = delay;
                }

                yield return null;
            }

            // do standard attack if no other attack was decided on
            if (_attackDecision == EnumAttackDecision.None)
            {
                _attackDecision = EnumAttackDecision.Normal;
            }

            // hide prompt if it was shown
            if (comboCounter > 2)
            {
                TorqueObjectDatabase.Instance.FindObject<T2DStaticSprite>("hud_superattackprompt" + player).Visible = false;
            }

            // fade out and shrink the floater until it's gone
            Vector2 sizeDiff = -floater.Size;
            baseSize = floater.Size;
            float alphaDiff = -floater.VisibilityLevel;
            float baseAlpha = floater.VisibilityLevel;
            targetSizeY = 0.0f;

            elapsed = 0.0f;
            float duration = 0.5f;

            while (elapsed < duration)
            {
                mul = elapsed / duration;

                floater.Size = baseSize + (sizeDiff * mul);
                floater.VisibilityLevel = baseAlpha + (alphaDiff * mul);

                elapsed += _dt;

                yield return null;
            }

            floater.MarkForDelete = true;   // we're done with the floater now
        }

        protected IEnumerator<AsyncTaskStatus> AsyncTask_DisplayComboGhost(int answer, float comboCounter, float sizeMultiplier, float mulSpeed)
        {
            // create a ghost text floater at the scoring answer's location
            T2DStaticSprite floater = TorqueObjectDatabase.Instance.CloneObject<T2DStaticSprite>("hud_scorefloater");
            floater.Components.FindComponent<MathMultiTextComponent>().TextValue = comboCounter.ToString() + "x";
            Assert.Fatal(floater.Folder == null, "ghost floater object is already in a Folder");
            floater.Folder = _sceneFolder;   // must create this object in our folder and not any overlayed scene's folder or when that scene is unloaded so is this object!
            TorqueObjectDatabase.Instance.Register(floater);

            yield return null;  // give the text component time to create it's texture

            // initialize the ghost's size and position
            Vector2 baseSize = floater.Size;
            floater.Position = new Vector2(_answerDisplays[answer].Position.X, _answerDisplays[answer].Position.Y - 50.0f);
            floater.Visible = true;
            floater.Layer++;

            SimpleMaterial material = (floater.Material as SimpleMaterial);
            material.IsColorBlended = true;
            material.IsTranslucent = true;

            // expand the ghost until it reaches it maximum size (at which point it will also be completely faded out)
            float targetSizeY = baseSize.Y * sizeMultiplier;
            float mul = 1.00f;

            while (floater.Size.Y < targetSizeY)
            {
                mul *= mulSpeed;
                floater.Size = baseSize * mul;
                floater.VisibilityLevel = 1.0f - ((floater.Size.Y - baseSize.Y) / (targetSizeY - baseSize.Y));
                yield return null;
            }

            floater.MarkForDelete = true;   // we're done with the ghost now
        }

        protected IEnumerator<AsyncTaskStatus> AsyncTask_FreezePlayer(int player)
        {
            // play the character's freeze animation
            AddAsyncTask(TutorManager.Instance.PlayAnim(player, Game.Instance.ActiveGameplaySettings.Players[player].Character, TutorManager.Tutor.EnumAnim.Freeze, _tutorSprites[player]), false);

            // wait until the character is unfrozen
            while (!_visuallyUnfreezePlayers[player]) yield return null;

            // now play the character's unfreeze
            if (_tutorSprites[player].AnimationData == TutorManager.Instance.GetAnimationData(player, Game.Instance.ActiveGameplaySettings.Players[player].Character, TutorManager.Tutor.EnumAnim.Freeze))
            {
                AddAsyncTask(TutorManager.Instance.PlayAnim(player, Game.Instance.ActiveGameplaySettings.Players[player].Character, TutorManager.Tutor.EnumAnim.Unfreeze, _tutorSprites[player]), false);
                _visuallyUnfreezePlayers[player] = false;

                // make sure to reset the player's input so any answer-button they happened to mash accidentally whilst frozen won't be taken as their answer and they get a chance to answer properly
                Game.Instance.ActiveGameplaySettings.Players[player].ResetButtonPressed();
                _gameplayData.ResetPlayerInput(player);
            }
            else
            {
                _visuallyUnfreezePlayers[player] = false;
                Debug.WriteLine("Visually unfreezing player: no animation played as player already visually in an unfrozen state");
            }
        }

        protected IEnumerator<AsyncTaskStatus> AsyncTask_DoClockExplosion()
        {
            RegisterWaiting("explodeclock");

            // set the clock display to 'empty' so the usual display isn't visible
            MathMultiTextComponent text = TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("hud_timerdigits").Components.FindComponent<MathMultiTextComponent>();
            text.TextValue = " ";
            _timerReveal.SweepAngle = 360.0f;

            // create and initialize x * y static sprites to act as explosion fragments to replace the usual display
            // we will also kick off the explosion fx while we're at it
            T2DStaticSprite[,] fragments = new T2DStaticSprite[8, 8];
            T2DSceneObject clockRing = _timerReveal.Owner as T2DSceneObject;
            Vector2 topleft = new Vector2(clockRing.Position.X - (clockRing.Size.Y * 0.5f), clockRing.Position.Y - (clockRing.Size.Y * 0.5f));

            for (int x = 0; x < 8; x++)
            {
                for (int y = 0; y < 8; y++)
                {
                    T2DStaticSprite fragment = TorqueObjectDatabase.Instance.CloneObject<T2DStaticSprite>("hudextra_fragmenttemplate");
                    fragment.Position = new Vector2(topleft.X + fragment.Size.X * x + fragment.Size.X * 0.5f, topleft.Y + fragment.Size.Y * y + fragment.Size.Y * 0.5f);
                    fragment.MaterialRegionIndex = y * 8 + x;
                    fragments[x, y] = fragment;
                    TorqueObjectDatabase.Instance.Register(fragment);

                    // kick off the explosion fx for this fragment
                    fragment.Components.FindComponent<ClockExplosionFragmentComponent>().StartFX((float)x - 3.5f, (float)y - 3.5f);
                }
            }

            // wait for a while before continuing with other stuff (note: it doesn't matter if fx isn't finished yet as the fragments will automatically delete themselves - we don't need to clean up that stuff)
            float elapsed = 0.0f;
            
            while (elapsed < 1.0f)
            {
                elapsed += _dt;
                yield return null;
            }

            UnRegisterWaiting("explodeclock");
        }

        protected void UpdateLevelNameAndQuestionInfoDisplay()
        {
            // Hide?
            if (_gameplayData == null || _gameplayData.Question == null)
            {
                //TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("hud_levelname").Visible = false;
                TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("hud_questioninfo").Visible = false;
            }
            // else update and show
            else
            {
                //T2DSceneObject levelName = TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("hud_levelname");
                //levelName.Visible = true;
                //levelName.Components.FindComponent<MathMultiTextComponent>().TextValue = _gameplayData.Question.LevelName.ToUpper();

                T2DSceneObject questionInfo = TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("hud_questioninfo");
                questionInfo.Visible = true;
                questionInfo.Components.FindComponent<MathMultiTextComponent>().TextValue = _gameplayData.Question.CatName.ToUpper();

                // TESTING... tell us what the parameters are that are being used
                //Debug.WriteLine("Question params from xml data: ");
                //Debug.WriteLine("----time allowed: " + _gameplayData.Question.TimeAllowed);
                //Debug.WriteLine("----AI min easy time: " + _gameplayData.Question.AIMinTime[0]);
                //Debug.WriteLine("----AI avg easy time: " + _gameplayData.Question.AIAvgTime[0]);
                //Debug.WriteLine("----AI max easy time: " + _gameplayData.Question.AIMaxTime[0]);
                //Debug.WriteLine("----AI min medium time: " + _gameplayData.Question.AIMinTime[1]);
                //Debug.WriteLine("----AI avg medium time: " + _gameplayData.Question.AIAvgTime[1]);
                //Debug.WriteLine("----AI max medium time: " + _gameplayData.Question.AIMaxTime[1]);
                //Debug.WriteLine("----AI min hard time: " + _gameplayData.Question.AIMinTime[2]);
                //Debug.WriteLine("----AI avg hard time: " + _gameplayData.Question.AIAvgTime[2]);
                //Debug.WriteLine("----AI max hard time: " + _gameplayData.Question.AIMaxTime[2]);
                //Debug.WriteLine("----AI min expert time: " + _gameplayData.Question.AIMinTime[3]);
                //Debug.WriteLine("----AI avg expert time: " + _gameplayData.Question.AIAvgTime[3]);
                //Debug.WriteLine("----AI max expert time: " + _gameplayData.Question.AIMaxTime[3]);
                //Debug.WriteLine("----Hint: " + _gameplayData.Question.Hint);
            }
        }

        protected void FreezePlayer(int playerNum)
        {
            // if already frozen out or the timer is already in the red zone then we don't need to do any freezing
            if (_isFrozen[playerNum] || !_isInMultiplierZone) return;

            // else we should definitely freeze them so that they can't abuse the hint feature for unfair advantage
            _isFrozen[playerNum] = true;
            AddAsyncTask(AsyncTask_FreezePlayer(playerNum), true);

            // sfx
            MFSoundManager.Instance.PlaySFX(MFSoundManager.EnumSFX.Freeze);
        }

        protected void UnFreezePlayer(int playerNum)
        {
            // can only unfreeze if frozen - duh!
            if (!_isFrozen[playerNum]) return;

            // else unfreeze them
            _visuallyUnfreezePlayers[playerNum] = true;
            _isFrozen[playerNum] = false;
        }
        
        private static void StopTimerSFX()
        {
            MFSoundManager.Instance.StopSFX(MFSoundManager.EnumSFX.TimerPart1);
            MFSoundManager.Instance.StopSFX(MFSoundManager.EnumSFX.TimerPart2);
            MFSoundManager.Instance.StopSFX(MFSoundManager.EnumSFX.TimerPart3);
        }
        


        /// <summary>
        /// This class encapsulates the gameplay data used in the game.
        /// Update() will be called on the gameplay data at the start of every tick.
        /// 
        /// This base class implementation can handle multiple local players.
        /// </summary>
        protected class GameplayData
        {
            public QuestionContent Question;
            public Player.EnumGamepadButton[] PlayerInput = new Player.EnumGamepadButton[2];
            public bool[] HintPressed = new bool[2];
            public bool[] SuperAttackPressed = new bool[2];
            public bool[] TauntPressed = new bool[2];
            public float CountDownRemaining_ToSend;
            public float CountDownRemaining_ToUse;


            public GameplayData()
            {
            }

            // returns true if the answer is blank
            public bool IsBlankAnswer(int answerNumber)
            {
                return Question.Answers[answerNumber] == MathQuestion.BLANK_ANSWER;
            }

            public virtual void InitializePlayer(int playerNum)
            {
                ResetPlayerInput(playerNum);
            }

            public virtual void UpdatePlayerInput(float countdownRemaining)
            {
                CountDownRemaining_ToSend = countdownRemaining;
                CountDownRemaining_ToUse = countdownRemaining;  // no difference here - difference is onlky in LIVE multiplayer

                // get inputs from all local players
                List<Player> players = Game.Instance.ActiveGameplaySettings.Players;

                for (int i = 0; i < players.Count; i++)
                {
                    if (!(players[i] is PlayerRemote))
                    {
                        PlayerInput[i] = players[i].GetButtonPressed();
                        HintPressed[i] = players[i].HintPressed;
                        SuperAttackPressed[i] = players[i].SuperAttackPressed;
                        TauntPressed[i] = players[i].TauntPressed;
                    }
                }
            }

            public virtual void ResetPlayerInput()
            {
                ResetPlayerInput(0);
                ResetPlayerInput(1);
            }

            public virtual void ResetPlayerInput(int playerNum)
            {
                PlayerInput[playerNum] = Player.EnumGamepadButton.None;
                HintPressed[playerNum] = false;
                SuperAttackPressed[playerNum] = false;
                TauntPressed[playerNum] = false;
            }

            public override string ToString()
            {
                string ret = "";

                if (Question == null)
                {
                    ret += "<no question>";
                }
                else
                {
                    ret += Question.ToString();
                }

                if (PlayerInput == null)
                {
                    ret += "<no player input>\n";
                }
                else
                {
                    for (int i = 0; i < PlayerInput.Length; i++)
                    {
                        ret += "Player[" + i + "] => " + PlayerInput[i] + "\n";
                    }
                }

                return ret;
            }
        }
    }
}
