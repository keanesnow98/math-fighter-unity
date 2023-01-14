using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GarageGames.Torque.T2D;



namespace MathFreak.GameStates.Transitions
{
    /// <summary>
    /// An instance of this class will manage triggering a set of objects to 'fly on' to the screen.
    /// The objects flying on must have the TransitionHorizontalFlyonComponent component attached.
    /// </summary>
    public class TransitionHorizontalFlyon
    {
        private List<TransitionHorizontalFlyOnComponent> _flyonObjs;
        private float _objFlyonDelay;    // time delay between objects starting to flyon
        private float _objFlyonDelayElapsed;
        private bool _hasPlayedSfx;

        public float ObjFlyonDelay
        {
            get { return _objFlyonDelay; }
            set { _objFlyonDelay = value; }
        }

        public TransitionHorizontalFlyon()
        {
            _flyonObjs = new List<TransitionHorizontalFlyOnComponent>();
        }

        public void Add(T2DSceneObject obj)
        {
            _flyonObjs.Add(obj.Components.FindComponent<TransitionHorizontalFlyOnComponent>());
            obj.Visible = false;
        }

        // returns true when all the objects have flown on and reported for activate duty
        public bool tick(float dt)
        {
            if (!_hasPlayedSfx)
            {
                _hasPlayedSfx = true;
                MFSoundManager.Instance.PlaySFX(MFSoundManager.EnumSFX.MenuFlyon);
            }

            // increment the timer that we use to trigger objects flying on
            _objFlyonDelayElapsed += dt;

            // check if it's time to tell another object to fly on
            if (_objFlyonDelayElapsed >= _objFlyonDelay)
            {
                _objFlyonDelayElapsed = 0.0f;

                // find the first object that isn't flying on or activated yet and tell it to fly on
                foreach (TransitionHorizontalFlyOnComponent obj in _flyonObjs)
                {
                    if (!obj.IsFlyingOn && !obj.IsActivated)
                    {
                        obj.FlyOn();
                        return false;   // no need to do any more processing here so just return
                    }
                }
            }

            // check if all the objects are active yet and return true if they are (the objects will continue to transition on by themselves, but as far as we are concerned the we are no longer needed once the objects are all reporting for active duty)
            foreach (TransitionHorizontalFlyOnComponent obj in _flyonObjs)
            {
                if (!obj.IsActivated)
                {
                    return false;   // no need to do any more processing here so just return
                }
            }

            // default returns true - if we've reached here then all objects have been told to fly on have activated
            return true;
        }

        public void CancelAll()
        {
            foreach (TransitionHorizontalFlyOnComponent obj in _flyonObjs)
            {
                obj.CancelFlyon();
            }
        }
    }
}
