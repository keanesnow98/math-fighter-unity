using System;
using System.Collections.Generic;
using System.Text;

using GarageGames.Torque.Core;

using MathFreak.GameStateFramework;
using MathFreak.GameStates;
using MathFreak.GameStates.Dialogs;
using System.Diagnostics;



namespace MathFreak.GUIFrameWork
{
    /// <summary>
    /// Repository for the actions that can be set for GUI components via the TX scene editor
    /// Add new actions here as required.
    /// </summary>
    [TorqueXmlSchemaType]
    public class GUIActions
    {
        // expose the actions to TX scene editor
        public static GUIActionDelegate PopGameState { get { return _popGameState; } }
        public static GUIActionDelegate GotoMainMenu { get { return _gotoMainMenu; } }
        public static GUIActionDelegate TellAFriend { get { return _tellAFriend; } }
        public static GUIActionDelegate QuitGame { get { return _quitGame; } }

        // the actual actions go here...
        private static GUIActionDelegate _popGameState = new GUIActionDelegate(delegate(GUIComponent guiComponent, string paramString)
        {
            Debug.WriteLine("(Action) Popping game state");
            GameStateManager.Instance.Pop();
        });

        private static GUIActionDelegate _gotoMainMenu = new GUIActionDelegate(delegate(GUIComponent guiComponent, string paramString)
        {
            Debug.WriteLine("(Action) Clearing and loading the main menu");
            GameStateManager.Instance.ClearAndLoad(GameStateNames.MAINMENU, null);
        });

        private static GUIActionDelegate _tellAFriend = new GUIActionDelegate(delegate(GUIComponent guiComponent, string paramString)
        {
            Debug.WriteLine("(Action) tell a friend");
            Game.Instance.TellAFriend();
        });
        
        private static GUIActionDelegate _quitGame = new GUIActionDelegate(delegate(GUIComponent guiComponent, string paramString)
        {
            Debug.WriteLine("(Action) quitting the game???");
            GameStateManager.Instance.PushOverlay(GameStateNames.DIALOG_QUIT, null);
            //(GameStateManager.Instance.CurrentObject as DialogQuit).OnYes();
        });
        
    //    private static GUIActionDelegate _pushPlayGameState = new GUIActionDelegate(delegate(GUIComponent guiComponent, string paramString)
    //    {
    //        Debug.WriteLine("(Action) push level chooser state");
    //        GameStateManager.Instance.Push(States.LEVELCHOOSER, null);
    //    });

    //    private static GUIActionDelegate _pushHighscoresGameState = new GUIActionDelegate(delegate(GUIComponent guiComponent, string paramString)
    //    {
    //        // NOTE: actually we are now clearing and loading this state
    //        Debug.WriteLine("(Action) clear and load highscores game state");
    //        GameStateManager.Instance.ClearAndLoad(States.HIGHSCORES, null);
    //    });

    //    private static GUIActionDelegate _pushOptionsGameState = new GUIActionDelegate(delegate(GUIComponent guiComponent, string paramString)
    //    {
    //        Debug.WriteLine("(Action) Pushing options game state");
    //        GameStateManager.Instance.Push(States.OPTIONS, null);
    //    });

    //    private static GUIActionDelegate _pushTutorialGameState = new GUIActionDelegate(delegate(GUIComponent guiComponent, string paramString)
    //    {
    //        Debug.WriteLine("(Action) Pushing tutorial game state");
    //        GameStateManager.Instance.Push(States.TUTORIAL, null);
    //    });

    //    private static GUIActionDelegate _pushCreditsGameState = new GUIActionDelegate(delegate(GUIComponent guiComponent, string paramString)
    //    {
    //        Debug.WriteLine("(Action) Pushing credits game state");
    //        GameStateManager.Instance.Push(States.CREDITS, null);
    //    });

    //    private static GUIActionDelegate _gotoQuitScreen = new GUIActionDelegate(delegate(GUIComponent guiComponent, string paramString)
    //    {
    //        Debug.WriteLine("(Action) pushing quit screen");
    //        GameStateManager.Instance.Push(States.QUIT, null);
    //    });

    //    private static GUIActionDelegate _nextTutorialPage = new GUIActionDelegate(delegate(GUIComponent guiComponent, string paramString)
    //    {
    //        Debug.WriteLine("(Action) next tutorial page");
    //        (GameStateManager.Instance.CurrentObject as GameStateTutorial).GotoNextPage();
    //    });

    //    private static GUIActionDelegate _pushPauseMenu = new GUIActionDelegate(delegate(GUIComponent guiComponent, string paramString)
    //    {
    //        Debug.WriteLine("(Action) pushing pause menu screen");

    //        (GameStateManager.Instance.CurrentObject as GameStatePlay).DoPauseMenu();
    //    });

    //    private static GUIActionDelegate _clearAndLoadMainMenu = new GUIActionDelegate(delegate(GUIComponent guiComponent, string paramString)
    //    {
    //        Debug.WriteLine("(Action) clear and load main menu");
    //        GameStateManager.Instance.ClearAndLoad(States.MAINMENU, null);
    //    });

    //    private static GUIActionDelegate _increaseMusicVolume = new GUIActionDelegate(delegate(GUIComponent guiComponent, string paramString)
    //    {
    //        Debug.WriteLine("(Action) increase music volume");

    //        (GameStateManager.Instance.CurrentObject as GameStateOptions).DoIncreaseMusicVolume();
    //    });

    //    private static GUIActionDelegate _decreaseMusicVolume = new GUIActionDelegate(delegate(GUIComponent guiComponent, string paramString)
    //    {
    //        Debug.WriteLine("(Action) decrease music volume");

    //        (GameStateManager.Instance.CurrentObject as GameStateOptions).DoDecreaseMusicVolume();
    //    });

    //    private static GUIActionDelegate _increaseSoundVolume = new GUIActionDelegate(delegate(GUIComponent guiComponent, string paramString)
    //    {
    //        Debug.WriteLine("(Action) increase sound volume");

    //        (GameStateManager.Instance.CurrentObject as GameStateOptions).DoIncreaseSoundVolume();
    //    });

    //    private static GUIActionDelegate _decreaseSoundVolume = new GUIActionDelegate(delegate(GUIComponent guiComponent, string paramString)
    //    {
    //        Debug.WriteLine("(Action) decrease sound volume");

    //        (GameStateManager.Instance.CurrentObject as GameStateOptions).DoDecreaseSoundVolume();
    //    });

    //    private static GUIActionDelegate _playLevel = new GUIActionDelegate(delegate(GUIComponent guiComponent, string paramString)
    //    {
    //        Debug.WriteLine("(Action) playing level: " + paramString);

    //        (GameStateManager.Instance.CurrentObject as GameStateLevelChooser).PlayLevel(paramString);
    //    });

    //    private static GUIActionDelegate _nameEntered = new GUIActionDelegate(delegate(GUIComponent guiComponent, string paramString)
    //    {
    //        Debug.WriteLine("(Action) name entered");
    //        (GameStateManager.Instance.CurrentObject as GameStateHighscores).OnNameEntered();
    //    });

    //    private static GUIActionDelegate _toggleFullscreen = new GUIActionDelegate(delegate(GUIComponent guiComponent, string paramString)
    //    {
    //        Debug.WriteLine("(Action) toggling fullscreen");

    //        Game.Instance.GraphicsDeviceManager.ToggleFullScreen();
    //        //Game.Instance.GraphicsDeviceManager.ApplyChanges();
    //    });

    //    private static GUIActionDelegate _leaveOptionsScreen = new GUIActionDelegate(delegate(GUIComponent guiComponent, string paramString)
    //    {
    //        Debug.WriteLine("(Action) Leaving options screen");
    //        (GameStateManager.Instance.CurrentObject as GameStateOptions).OnLeaveOptionsScreen();
    //    });
    }

    public delegate void GUIActionDelegate(GUIComponent guiComponent, string paramString);
}
