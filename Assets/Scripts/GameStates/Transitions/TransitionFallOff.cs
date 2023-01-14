using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GarageGames.Torque.T2D;



namespace MathFreak.GameStates.Transitions
{
    /// <summary>
    /// An instance of this class will manage triggering a set of objects to 'fall off' the screen.
    /// The objects falling off must have the TransitionFallOffComponent component attached.
    /// </summary>
    public class TransitionFallOff
    {
        private List<TransitionFallOffComponent> _falloffObjs;
        private float _objFalloffDelay;    // time delay between objects starting to fall off
        private float _objFalloffDelayElapsed;
        private bool _hasPlayedSfx;

        public float ObjFalloffDelay
        {
            get { return _objFalloffDelay; }
            set { _objFalloffDelay = value; }
        }

        public TransitionFallOff()
        {
            _falloffObjs = new List<TransitionFallOffComponent>();
        }

        public void Add(T2DSceneObject obj)
        {
            _falloffObjs.Add(obj.Components.FindComponent<TransitionFallOffComponent>());
        }

        // returns true when all the objects report that they have fallen far enough to count as having
        // 'fallen off' the screen.  Note that in practice objects may report reaching this state before
        // they are actually off screen proper - this is so that any stuff waiting to transition on can
        // get transitioning.
        public bool tick(float dt)
        {
            if (!_hasPlayedSfx)
            {
                _hasPlayedSfx = true;
                MFSoundManager.Instance.PlaySFX(MFSoundManager.EnumSFX.MenuFalloff);
            }

            // increment the timer that we use to trigger objects flying on
            _objFalloffDelayElapsed += dt;

            // check if it's time to tell another object to fly on
            if (_objFalloffDelayElapsed >= _objFalloffDelay)
            {
                _objFalloffDelayElapsed = 0.0f;

                // find the first object that isn't falling off or 'exited' yet and tell it to fall off
                foreach (TransitionFallOffComponent obj in _falloffObjs)
                {
                    if (!obj.IsFallingOff && !obj.HasExited)
                    {
                        obj.FallOff();
                        return false;   // no need to do any more processing here so just return
                    }
                }
            }

            // check if all the objects have 'exited' and return true if they have (the objects will continue to transition off by themselves, but as far as we are concerned the we are no longer needed once the objects are all reporting they have 'exited')
            foreach (TransitionFallOffComponent obj in _falloffObjs)
            {
                if (!obj.HasExited)
                {
                    return false;   // no need to do any more processing here so just return
                }
            }

            // default returns true - if we've reached here then all objects have been told to fall off have 'exited'
            return true;
        }
    }
}
