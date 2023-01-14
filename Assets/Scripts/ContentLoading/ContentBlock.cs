using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GarageGames.Torque.Core;
using System.Threading;
using System.Diagnostics;



namespace MathFreak.ContentLoading
{
    /// <summary>
    /// A ContentBlock is basically a list of assets we want a to load/unload.
    /// Having ContentBlocks makes it easier to manage content loading/unloading - just set up
    /// the content blocks that you want and then tell the blocks to load/unload their content
    /// as and when you require dynamically loading/unloading content in your game - simples! =)
    /// 
    /// Content can be specified as whole folders (with or without recursion) and as individual
    /// assets.
    /// </summary>
    public class ContentBlock
    {
        protected ContentLoader _contentLoader;
        protected AsyncContentBlockResult _result;

        protected List<FolderInfo> _folders = new List<FolderInfo>();
        protected List<AssetInfo> _assets = new List<AssetInfo>();

        public AsyncContentBlockResult AsyncResult
        {
            get { return _result; }
        }


        public ContentBlock()
        {
            _contentLoader = new ContentLoader(Game.Instance.Services, ResourceManager.Instance.CurrentContentManager.RootDirectory);
        }

        ~ContentBlock()
        {
            if (_contentLoader != null)
            {
                _contentLoader.DisposeAll();
                _contentLoader = null;
            }

            _folders = null;
            _assets = null;
        }

        public void Dispose()
        {
            if (_contentLoader != null)
            {
                _contentLoader.DisposeAll();
                _contentLoader = null;
            }

            _folders = null;
            _assets = null;
        }

        // Note: folders must be terminated with a "/" or "\"
        public void AddFolder<T>(string folderPath, bool recursive)
        {
            Assert.Fatal(folderPath.EndsWith("/") || folderPath.EndsWith(@"\"), @"Path must end in either \ or /");

            // NOTE: we aren't checking for duplicates - it won't cause a crash if there are duplicates and saves searching the list all the time
            _folders.Add(new FolderInfo
            {
                FolderName = folderPath,
                Recursive = recursive,
                AssetType = typeof(T)
            });
        }

        public void AddAsset<T>(string assetPath)
        {
            // NOTE: we aren't checking for duplicates - it won't cause a crash if there are duplicates and saves searching the list all the time
            _assets.Add(new AssetInfo
            {
                AssetName = assetPath,
                AssetType = typeof(T)
            });
        }

        // Call this if you just want the assets in this content block to be allowed to load, but
        // not to be actually loaded until they are requested.
        public void AllowLoading()
        {
            AllowLoading(null);
        }

        private void AllowLoading(ProgressMeter progress)
        {
            // register all individually specified assets
            int count = _assets.Count;

            for (int i = 0; i < count; i++)
            {
                AssetInfo assetInfo = _assets[i];
                _contentLoader.RegisterAsset(assetInfo.AssetType, assetInfo.AssetName);
            }

            // register all assets in specified folders
            count = _folders.Count;

            for (int i = 0; i < count; i++)
            {
                FolderInfo folderInfo = _folders[i];
                _contentLoader.RegisterAssets(folderInfo.AssetType, folderInfo.FolderName, folderInfo.Recursive, progress);
            }
        }

        // Call this if you just want the assets in this content block to be allowed to load, but
        // not to be actually loaded until they are requested.
        public AsyncContentBlockResult AsyncAllowLoading(AsyncCallback callback, object asyncObject)
        {
            Assert.Fatal(_result == null || _result.IsCompleted, "AsyncAllowLoading() - the content block is currently already in the middle of an asynchronous operation");

            _result = new AsyncContentBlockResult(asyncObject);

            // create the thread and the code that will run when the thread is started
            ThreadStart ThreadStarter = delegate
            {
#if XBOX
                Thread.CurrentThread.SetProcessorAffinity(4);
#endif
                // allow loading
                AllowLoading(_result.ProgressMeter);

                // mark as finished and call the call back if there is one
                _result.MarkAsCompleted();

                if (callback != null)
                {
                    callback(_result);
                }
            };

            Thread myThread = new Thread(ThreadStarter);

            // start the thread
            myThread.Start();

            return _result;
        }

        // Call this if you want to immediately load the assets in this content block.
        public void LoadImmediately()
        {
#if DEBUG
            ProgressMeter progress = new ProgressMeter();
            _contentLoader.PreLoadRegisteredAssets(progress);
            Debug.WriteLine("Texture bytes loaded into memory: " + progress.TextureBytesInMemory);
            Debug.WriteLine("Total texture bytes in memory: " + (ContentLoader.TextureBytesInMemory));
#else
            _contentLoader.PreLoadRegisteredAssets(null);
#endif
        }

        // Call this if you want to immediately load the assets in this content block.
        public AsyncContentBlockResult AsyncLoadImmediately(AsyncCallback callback, object asyncObject)
        {
            Assert.Fatal(_result == null || _result.IsCompleted, "AsyncLoadImmediately() - the content block is currently already in the middle of an asynchronous operation");
            _result = new AsyncContentBlockResult(asyncObject);

            // create the thread and the code that will run when the thread is started
            ThreadStart ThreadStarter = delegate
            {
#if XBOX
                Thread.CurrentThread.SetProcessorAffinity(4);
#endif

                // load assets
                _contentLoader.PreLoadRegisteredAssets(_result.ProgressMeter);

#if DEBUG
                Debug.WriteLine("Texture bytes loaded into memory: " + _result.ProgressMeter.TextureBytesInMemory);
                Debug.WriteLine("Total texture bytes in memory: " + (ContentLoader.TextureBytesInMemory));
#endif

                // mark as finished and call the call back if there is one
                _result.MarkAsCompleted();

                if (callback != null)
                {
                    callback(_result);
                }
            };

            Thread myThread = new Thread(ThreadStarter);

            // start the thread
            myThread.Start();

            return _result;
        }

        // Call this to disallow any further loading of assets in this content block.  Does not
        // actually unload the assets though, so any existing references to the assets are still
        // valid - just that no new references to the assets will be obtainable via the content
        // loader.
        public void DisallowLoading()
        {
            _contentLoader.UnregisterAllAssets();
        }

        // Call this to unload all assets in the content block.  This will also make it so that
        // none of the assets in the content block are allowed to load any longer (NOTE: if another
        // content block is allowing a given asset to load then ultimately that asset can
        // still be loaded - 'disallowing' of loading only applies to the present content block).
        public void UnloadImmediately()
        {
            _contentLoader.Unload();
            Debug.WriteLine("Total texture bytes in memory: " + (ContentLoader.TextureBytesInMemory));
        }

        // Call this to load a particular asset using the content block's content loader.  You should
        // only need this in the special situation where more than one content block has the same asset
        // registered AND you are obtaining the asset directly rather than via the TX resource manager!
        //
        // NOTE: the asset being requested must have belong to this content block and AllowLoading() must
        // have been called on this content block or the asset will not be loaded.
        public T LoadAsset<T>(string assetName)
        {
            return _contentLoader.LoadAssetDirectly<T>(assetName);
        }



        public class ProgressMeter
        {
            public int ObjectCount;
            public int TextureBytesInMemory;
        }



        protected class FolderInfo
        {
            public string FolderName;
            public bool Recursive;
            public Type AssetType;
        }



        protected class AssetInfo
        {
            public string AssetName;
            public Type AssetType;
        }
    }



    public class AsyncContentBlockResult : IAsyncResult
    {
        protected bool _isCompleted = false;
        protected object _asyncObject = null;
        protected ContentBlock.ProgressMeter _progressMeter = new ContentBlock.ProgressMeter();

        public virtual object AsyncState { get { return _asyncObject; } }
        public virtual WaitHandle AsyncWaitHandle { get { return null; } }
        public virtual bool CompletedSynchronously { get { return false; } }
        public virtual bool IsCompleted { get { return _isCompleted; } }
        public virtual ContentBlock.ProgressMeter ProgressMeter { get { return _progressMeter; } }


        public AsyncContentBlockResult(object asyncObject)
        {
            _asyncObject = asyncObject;
        }

        internal void MarkAsCompleted()
        {
            _isCompleted = true;
        }
    }
}
