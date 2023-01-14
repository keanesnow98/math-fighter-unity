using System;
using System.Collections.Generic;
////using System.Linq;
using System.Text;



namespace MathFreak
{
    /// <summary>
    /// Calling PreDraw() on this manager from Game.Draw() before TX does it's drawing stuff
    /// allows our own stuff to do things before TX drawing takes place.  E.g. the TextMaterial
    /// class uses this opportunity to update it's texture if it needs to, before the material
    /// is actually used by TX to render an object.
    /// </summary>
    public class PreDrawManager
    {
        private static PreDrawManager _instance;

        private List<IPreDraw> _items;

        public static PreDrawManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new PreDrawManager();
                }

                return _instance;
            }
        }

        protected PreDrawManager()
        {
            _items = new List<IPreDraw>();
        }

        public void Register(IPreDraw text)
        {
            _items.Add(text);
        }

        public void UnRegister(IPreDraw text)
        {
            _items.Remove(text);
        }

        // Called by Game to give allow stuff to happen before drawing - e.g. Text components can update their text material before anything gets rendered to the screen
        public void PreDraw(float dt)
        {
            foreach (IPreDraw item in _items)
            {
                item.PreDraw(dt);
            }
        }
    }



    public interface IPreDraw
    {
        void PreDraw(float dt);
    }
}
