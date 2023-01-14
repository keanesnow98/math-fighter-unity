using System;
using System.Collections.Generic;
////using System.Linq;
using System.Text;

using GarageGames.Torque.Sim;
using GarageGames.Torque.Core;
using GarageGames.Torque.T2D;

using MathFreak.GUIFrameWork;
using System.Diagnostics;
using GarageGames.Torque.Materials;
using GarageGames.Torque.Core.Xml;


namespace MathFreak.GameStateFramework
{
    /// <summary>
    /// A gamestate object represents a specific gamestate in the game, such as a menu screen,
    /// the main gameplay screen, splashscreen, credits, etc...
    /// </summary>
    public abstract class GameState
    {
        // some commonly used constants for passing as params when loading gamestates
        public const string RESET_LASTSELECTED = "reset_lastselected";  // if a gamestate is keeping track of what was most recently selected (e.g. a menu item on a menu screen) then this tells it to ignore any stored 'last selection' and use the default instead

        //protected TorqueFolder _folder;
        //private List<HiddenObjectInfo> _hiddenObjects;

        private string _paramString;

        protected string ParamString
        {
            get { return _paramString; }
        }

        /// <summary>
        /// one time initialization - generic initialization tasks should be done here (normally
        /// this will *not* include loading scene files - and certainly not substantially sized resources,
        /// which should always be loaded via a dedicated resource loading gamestate that gives
        /// loading status feedback to the player)
        /// </summary>
        public virtual void Init() { }

        /// <summary>
        /// Called when a gamestate should prepare itself for transitioning onto the screen.
        /// A game state might load data or create objects, setup variables, etc.  Many game
        /// states will treat this like an OnLoad() type method call and 'load' their stuff
        /// before also preparing transitioning stuff.  NOTE: this method call should not load
        /// substantial data from a storage device - that should be should be done using a
        /// loading screen (via a gamestate specifically for loading the resource/s in question,
        /// and which shows loading progress to the player).
        /// </summary>
        public virtual void PreTransitionOn(string paramString) { _paramString = paramString; _SetParentFolder(); _PushGUI(); }

        /// <summary>
        /// Called when the gamestate should prepare for transitioning off the screen.
        /// </summary>
        public virtual void PreTransitionOff() { _PopGUI(); }

        /// <summary>
        /// Called once the transitions for both transitioning gamestates have completed.
        /// </summary>
        public virtual void OnTransitionOnCompleted() { }

        /// <summary>
        /// Called once the transitions for both transitioning gamestates have completed.
        /// Many gamestates may wish to use this point to do any 'unload' type clean up that
        /// has not already been done by this point.  In particular, of course, they will want
        /// to clean up any transition stuff that needs cleaning up, but also there may be general
        /// gamestate stuff that could not be cleaned up prior to this method being called.
        /// </summary>
        public virtual void OnTransitionOffCompleted() { }

        /// <summary>
        /// Ticks the transitioning of the gamestate's display onto the screen
        /// </summary>
        /// <returns>Returns true if the transition has completed, else returns false</returns>
        public virtual bool TickTransitionOn(float dt, bool prevStateHasTransitionedOff) { return true; }

        /// <summary>
        /// Ticks the transitioning of the gamestate's display off to the screen
        /// </summary>
        /// <returns>Returns true if the transition has completed, else returns false</returns>
        public virtual bool TickTransitionOff(float dt) { return true; }

        /// <summary>
        /// Called when the gamestate has been made a background gamestate (another gamestate
        /// is active over the top of this one, but this one will still get ticked, although it's
        /// GUI will have been disabled)
        /// </summary>
        public virtual void OnSetAsBackground()
        {
        }

        /// <summary>
        /// Called when the gamestate was a background state, but has now been restored to the
        /// 'foreground' (is the active state - it's GUI will have been reactivated).
        /// </summary>
        public virtual void OnSetAsForeground()
        {
        }

        /// <summary>
        /// Ticks the gamestate (when not transitioning on/off)
        /// </summary>
        public virtual void Tick(float dt) { }

        /// <summary>
        /// Called to get the gamestate to unload itself straight away (no transitioning).
        /// This is typically called to unload gamestates that have been overlayed and need to
        /// be unloaded, but won't get the transitioning stuff going on, so wouldn't otherwise
        /// have a chance to unload themselves.
        /// 
        /// NOTE: gamestates should not delegate to this method as it may result too many gui's being popped from the gui stack
        /// (e.g. don't delegate from OnTransitionOffCompleted - if req then create a third function that both this and
        /// OnTransitionOffCompleted delegate to)
        /// </summary>
        public virtual void UnloadedImmediately() { _PopGUI(); }

        /// <summary>
        /// Makes sure new torque objects are registered with a folder instance unique to this gamestate or any SceneLoader.Unload() called
        /// by another gamestate after this (e.g. in their OnTransitionOffCompleted() handler) would cause
        /// our stuff to be unloaded - obviously we don't want that!
        /// </summary>
        //protected virtual void _CreateFolder()
        //{
        //    _folder = new TorqueFolder();
        //    _folder.Name = "folder" + GameStateManager.Instance.GetNameOf(this);
        //    TorqueObjectDatabase.Instance.CurrentFolder = TorqueObjectDatabase.Instance.RootFolder; // need to register the folder before we can set it as the current folder - register the new folder with the rootFolder as current so that it is not unregistered unless we want it to be (if we registered it with whatever folder happens to be current then when that folder [which probably belongs to another gamestate] is unregistered so will our folder be!)
        //    TorqueObjectDatabase.Instance.Register(_folder);
        //    TorqueObjectDatabase.Instance.CurrentFolder = _folder;
        //}

        // make sure the gamestate scene's folder is nested in the Root folder and not another scene's folder
        // otherwise if that scene is unloaded it will unload our gamestate scene stuff too, which is definitely
        // not what we want.
        protected virtual void _SetParentFolder()
        {
            TorqueObjectDatabase.Instance.CurrentFolder = TorqueObjectDatabase.Instance.RootFolder;
        }

        protected virtual void _PushGUI()
        {
            Debug.WriteLine("Pushing GUI for: " + GameStateManager.Instance.GetNameOf(this));
            GUIManager.Instance.PushGUI();
        }

        protected virtual void _PopGUI()
        {
            Debug.WriteLine("Popping GUI for: " + GameStateManager.Instance.GetNameOf(this));
            GUIManager.Instance.PopGUI();
        }

        // gamestates might be not be in a position to quit right away so need the opportunity
        // to veto the invite (at least temporarily)
        public virtual bool CanAcceptInvite()
        {
            return true;
        }

        // gamestates that need to do something such as setting an isExiting flag should override this
        public virtual void OnInviteAccepted()
        {
        }

        //// Utility method that will set all visible objects in the gamestate's torque folder to non-visible
        //// typically used by a gamestate that needs to hide itself when pushing an overlay.
        //protected void _Hide()
        //{
        //    if (_hiddenObjects == null)
        //    {
        //        _hiddenObjects = new List<HiddenObjectInfo>();
        //    }

        //    _Hide(_folder);
        //}

        //private void _Hide(TorqueFolder folder)
        //{
        //    foreach (TorqueObject obj in folder)
        //    {
        //        if (obj is TorqueFolder)
        //        {
        //            _Hide(obj as TorqueFolder);
        //        }
        //        else if (obj is T2DSceneObject)
        //        {
        //            HiddenObjectInfo hidden = new HiddenObjectInfo();

        //            hidden.Obj = (obj as T2DSceneObject);
        //            hidden.WasVisible = hidden.Obj.Visible;
        //            hidden.Obj.Visible = false;

        //            _hiddenObjects.Add(hidden);
        //        }
        //    }
        //}

        //// Utility method that will unhide all the objects hidden by a call to _Hide()
        //// typically used by a gamestate that needs to unhide itself after an overlayed gamestate has been removed.
        //protected void _UnHide()
        //{
        //    foreach (HiddenObjectInfo hidden in _hiddenObjects)
        //    {
        //        hidden.Obj.Visible = hidden.WasVisible;
        //    }

        //    _hiddenObjects.Clear();
        //}

        //private struct HiddenObjectInfo
        //{
        //    public T2DSceneObject Obj;
        //    public bool WasVisible;
        //}
    }
}
