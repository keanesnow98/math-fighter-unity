using System;
using System.Collections.Generic;
////using System.Linq;
using System.Text;
using GarageGames.Torque.Core;
using System.Diagnostics;



namespace MathFreak
{
    /// <summary>
    /// A pool of reusable LogicalRenderTexture instances.  Pooling these instances means a big reduction
    /// in creation/disposal of rendertargets and their associated textures and thus less garbage collection 
    /// activity.
    /// </summary>
    public class LRTPool
    {
        public const int MIN_WIDTHPOWER = 5;
        public const int MIN_HEIGHTPOWER = 5;
        public const int MAX_WIDTHPOWER = 10;
        public const int MAX_HEIGHTPOWER = 10;
        public const int RANGE_WIDTHINDEX = (MAX_WIDTHPOWER - MIN_WIDTHPOWER) + 1;
        public const int RANGE_HEIGHTINDEX = (MAX_HEIGHTPOWER - MIN_HEIGHTPOWER) + 1;

        private static LRTPool _instance;

        public static LRTPool Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new LRTPool();
                }

                return _instance;
            }
        }

        private List<LogicalRenderTexture>[,] _available = new List<LogicalRenderTexture>[RANGE_WIDTHINDEX, RANGE_HEIGHTINDEX];
        private List<LogicalRenderTexture> _used = new List<LogicalRenderTexture>();

#if DEBUG
        private int _peakUsage;
#endif


        protected LRTPool()
        {
        }

        public void Init()
        {
            // TODO: precreate LRTs here if this is required for performance reasons
        }

        /// <summary>
        /// Disposes all pooled LRTs, including LRTs that are 'in use' (i.e. that have been acquired,
        /// but not released).
        /// </summary>
        public void Dispose()
        {
            foreach (LogicalRenderTexture lrt in _used)
            {
                _available[lrt.WidthIndex, lrt.HeightIndex].Add(lrt);
            }

            _used.Clear();

#if DEBUG
            Debug.WriteLine("LRT Pool Stats:");

            int lrtCount = 0;
            int lrtTotalTextureSize = 0;
#endif

            for (int i = 0; i < RANGE_WIDTHINDEX; i++)
            {
                for (int j = 0; j < RANGE_HEIGHTINDEX; j++)
                {
                    List<LogicalRenderTexture> bin = _available[i, j];

                    if (bin != null)
                    {
#if DEBUG
                        Debug.WriteLine("....[" + i + "," + j + "]...(" + (1 << (i + MIN_WIDTHPOWER)) + "x" + (1 << (j + MIN_HEIGHTPOWER)) + ") count: " + bin.Count);

                        lrtCount += bin.Count;
                        lrtTotalTextureSize += (1 << (i + MIN_WIDTHPOWER)) * (1 << (j + MIN_HEIGHTPOWER)) * 4;
#endif

                        foreach (LogicalRenderTexture lrt in bin)
                        {
                            lrt.Dispose();
                        }
                    }
#if DEBUG
                    else
                    {
                        Debug.WriteLine("....[" + i + "," + j + "]...(" + (1 << (i + MIN_WIDTHPOWER)) + "x" + (1 << (j + MIN_HEIGHTPOWER)) + ") count: none");
                    }
#endif

                    _available[i, j] = null;
                }
            }

#if DEBUG
            Debug.WriteLine("....\n....Total LRT count: " + lrtCount);
            Debug.WriteLine("....Peak Simultaneous LRT Usage: " + _peakUsage);
            Debug.WriteLine("....Total LRT texture size allocated: " + string.Format("{0:0,0,0}", lrtTotalTextureSize));
#endif
        }

        public LogicalRenderTexture AcquireLRT(int width, int height)
        {
            // convert the width/height to an index into our 2D array of LRT bins
            int widthIndex = GetIndexFromWidth(width);
            int heightIndex = GetIndexFromHeight(height);

            // if no bin then create one
            if (_available[widthIndex, heightIndex] == null)
            {
                _available[widthIndex, heightIndex] = new List<LogicalRenderTexture>();
            }

            // get the bin
            List<LogicalRenderTexture> bin = _available[widthIndex, heightIndex];

            // if no LRT in the bin then create a new one
            if (bin.Count == 0)
            {
                bin.Add(new LogicalRenderTexture(widthIndex + MIN_WIDTHPOWER, heightIndex + MIN_HEIGHTPOWER));
            }

            // get the requested LRT and ready it for use
            LogicalRenderTexture lrt = bin[0];
            bin.RemoveAt(0);
            _used.Add(lrt);
            lrt.SetLogicalSize(width, height);

#if DEBUG
            if (_used.Count > _peakUsage)
            {
                _peakUsage = _used.Count;
            }

            //Debug.WriteLine("peak: " + _peakUsage);
#endif

            // return the requested LRT
            return lrt;
        }

        public void ReleaseLRT(LogicalRenderTexture lrt)
        {
            if (lrt.RenderTarget == null) return;   // not an LRT created by the pool

            if (_used.Remove(lrt))
            {
                //Debug.WriteLine("Releasing LRT: " + _used.Count);
                _available[lrt.WidthIndex, lrt.HeightIndex].Add(lrt);
            }
            else
            {
                Assert.Fatal(false, "Trying to release an LRT that because the LRT is not in use!");
            }
        }

        private int GetIndexFromWidth(int width)
        {
            for (int i = MIN_WIDTHPOWER; i <= MAX_WIDTHPOWER; i++)
            {
                if ((width >> i) == 0)
                {
                    return i - MIN_WIDTHPOWER;
                }
            }

            Assert.Warn(false, "LRTPool - width is too big: " + width + ".  Width should be no greater than: " + (1 << MAX_WIDTHPOWER));
            return RANGE_WIDTHINDEX - 1;    // return the index for the biggest texture size we support so at least fit as much in as possible.
        }

        private int GetIndexFromHeight(int height)
        {
            for (int i = MIN_HEIGHTPOWER; i <= MAX_HEIGHTPOWER; i++)
            {
                if ((height >> i) == 0)
                {
                    return i - MIN_HEIGHTPOWER;
                }
            }

            Assert.Warn(false, "LRTPool - height is too big: " + height + ".  Height should be no greater than: " + (1 << MAX_HEIGHTPOWER));
            return RANGE_HEIGHTINDEX - 1;    // return the index for the biggest texture size we support so at least fit as much in as possible.
        }
    }
}
