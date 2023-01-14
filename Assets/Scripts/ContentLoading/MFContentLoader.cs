//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using Microsoft.Xna.Framework.Content;
//using System.Diagnostics;
//using GarageGames.Torque.Core;
//using System.Threading;
//using System.IO;
//using Microsoft.Xna.Framework.Graphics;
//using GarageGames.Torque.Util;



//namespace MathFreak.ContentLoading
//{
//    /// <summary>
//    /// We need to do some custom stuff with content loading - such as being able to query if an asset is
//    /// loaded yet rather than the default behaviour which is to load and not query.
//    /// 
//    /// NOTE: Because of the way TX objects are created on deserialization and to keep our contentloader
//    /// separate from the TX engine, the content loader will only load assets listed for loading - that is,
//    /// the usual Load method will only delegate to the base class if the asset is in the registered-assets list.
//    /// A new RegisterAsset method is provided that adds an asset to the list.
//    /// This means that when the TX resource manager tries to 'load' an asset then it will get null unless we
//    /// explicitly registered the asset for loading (we are no longer letting TX load stuff on it's own
//    /// schedule - we are taking full control of asset loading).  Modifications to the TX code to handle the nulll
//    /// asset have been added to the engine in the appropriate places - search for "DBC - HANDLE NULL ASSET" to
//    /// find the modifications.
//    /// 
//    /// NOTE: In addition to each loader's own list of the assets it has registered with it, there is also
//    /// a shared asset list.  This means that when an asset is requested the caller does not need to know
//    /// which loader loaded the asset (the asset's entry in the shared list has a reference to the loader
//    /// that loaded it).  It also means there doesn't need to be duplicates of assets loaded - if a loader
//    /// unloads it's assets then that is fine, because if another loader has the asset registered then the
//    /// asset will be loaded again if it is requested.
//    /// </summary>
//    public class MFContentLoader : ContentManager
//    {
//        private static Dictionary<string, MFContentLoader> _registeredSharedAssets = new Dictionary<string, MFContentLoader>();
//        private Dictionary<string, object> _registeredMyAssets = new Dictionary<string, object>();
//        private bool _finishedLoading;
//        private int _bytesLoaded;
        
//        public bool FinishedLoading
//        {
//            get { return _finishedLoading; }
//        }


//        public MFContentLoader(IServiceProvider sp)
//            : base(sp)
//        {
//        }

//        public MFContentLoader(IServiceProvider sp, string rootDir)
//            : base(sp, rootDir)
//        {
//        }

//        public override T Load<T>(string assetName)
//        {
//            // if the asset is registered globally then load it
//            if (_registeredSharedAssets.ContainsKey(assetName))
//            {
//                //Debug.WriteLine("....Asset is globally registered: " + assetName);
//                return _registeredSharedAssets[assetName].LoadAsset<T>(assetName);
//            }
//            // else if we can successfully register it as a global asset (will be successful if any of the active content loaders have the asset registered)
//            // then load it
//            else if (RegisterSharedAsset(assetName))
//            {
//                //Debug.WriteLine("....Asset is PRIVATELY registered: " + assetName);
//                return Load<T>(assetName);
//            }
//            // else it can't be loaded
//            else
//            {
//                //Debug.WriteLine("....Asset load request ignored: " + assetName);
//                return default(T);
//            }
//        }

//        private T LoadAsset<T>(string assetName)
//        {
//            try
//            {
//                return base.Load<T>(assetName);
//            }
//            catch (Exception e)
//            {
//                Assert.Fatal(false, "Failed to load asset: " + assetName);
//                Assert.Fatal(false, "....Exception:\n" + e.Message);
//                return default(T);
//            }
//        }

//        public void RegisterAsset(string assetName)
//        {
//            // register the asset in our private list
//            if (!_registeredMyAssets.ContainsKey(assetName))
//            {
//                //Debug.WriteLine("Registering my Asset: " + assetName);
//                _registeredMyAssets.Add(assetName, null);
//            }
//        }

//        /// <summary>
//        /// Returns true if the asset was successfully registered in the global list - i.e. any one of the
//        /// active content loaders had the asset registered privately and thus the asset was eligible
//        /// for registering globally.
//        /// </summary>
//        private static bool RegisterSharedAsset(string assetName)
//        {
//            foreach (KeyValuePair<string, MFContentLoader> loader in Game.Instance.ContentLoaders)
//            {
//                if (loader.Value._registeredMyAssets.ContainsKey(assetName))
//                {
//                    _registeredSharedAssets.Add(assetName, loader.Value);
//                    return true;
//                }
//            }

//            return false;
//        }

//        protected override void Dispose(bool disposing)
//        {
//            base.Dispose(disposing);
//        }

//        // Could use 'new' keyword but we don't own the underlying implementation and don't want to mess it up at all
//        public void DisposeAll()
//        {
//            Dispose(true);
//        }

//        public override void Unload()
//        {
//            base.Unload();
//            UnregisterAllAssets();
//        }

//        public void UnregisterAllAssets()
//        {
//            // unregister all our assets from the shared list
//            foreach (KeyValuePair<string, object> entry in _registeredMyAssets)
//            {
//                // only remove assets that *we* loaded - don't remove assets that other loaders loaded!
//                if (_registeredSharedAssets.ContainsKey(entry.Key) && _registeredSharedAssets[entry.Key] == this)
//                {
//                    //Debug.WriteLine("Unregistering from the global asset list: " + entry.Key);
//                    _registeredSharedAssets.Remove(entry.Key);
//                }
//#if DEBUG
//                //else
//                //{
//                //    if (!_registeredSharedAssets.ContainsKey(entry.Key))
//                //    {
//                //        Debug.WriteLine("Not unregistered from the global asset list (we didn't load it): " + entry.Key);
//                //    }
//                //    else
//                //    {
//                //        Debug.WriteLine("Not unregistered from the global asset list (it isn't in the list anyway): " + entry.Key);
//                //    }
//                //}
//#endif
//            }

//            // clear our private list
//            _registeredMyAssets.Clear();
//        }

//        public void UnregisterAsset(string assetName)
//        {
//            // only remove assets that *we* loaded - don't remove stuff other loaders loaded!
//            if (_registeredSharedAssets.ContainsKey(assetName) && _registeredSharedAssets[assetName] == this)
//            {
//                _registeredSharedAssets.Remove(assetName);
//            }

//            // remove from private list
//            _registeredMyAssets.Remove(assetName);
//        }

//        public void LoadAssets(string[] textureFolders, bool[] texturesRecursive, string[] fontFolders, bool[] fontsRecursive)
//        {
//            _finishedLoading = false;

//            // create the thread and the code that will run when the thread is started
//            ThreadStart ThreadStarter = delegate
//            {
//#if XBOX
//                // On Xbox we can set the thread to run on a specific core so the content doesn't interrupt any loading animations or other processes as much
//                Thread.CurrentThread.SetProcessorAffinity(4);
//#endif

//                _bytesLoaded = 0;

//                // load all the textures
//                if (textureFolders != null)
//                {
//                    for (int i = 0; i < textureFolders.Length; i++)
//                    {
//                        LoadTextures(textureFolders[i], texturesRecursive[i]);
//                    }
//                }

//                // load all the fonts
//                if (fontFolders != null)
//                {
//                    for (int i = 0; i < fontFolders.Length; i++)
//                    {
//                        LoadFonts(fontFolders[i], fontsRecursive[i]);
//                    }
//                }

//                Debug.WriteLine("Finished loading Assets - estimated texture memory allocation: " + string.Format("{0:0,0,0}", _bytesLoaded));
//                Debug.WriteLine("(NOTE: does not include memory allocation for fonts!)");

//                // finished
//                _finishedLoading = true;
//            };

//            Thread myThread = new Thread(ThreadStarter);

//            // start the thread
//            myThread.Start();
//        }

//        private void LoadTextures(string folderName, bool recursive)
//        {
//            //Debug.WriteLine("Loading TEXTURES from folder: " + folderName);

//            DirectoryInfo folderInfo = new DirectoryInfo(folderName);

//            // find all assets and load them
//            FileInfo[] files = folderInfo.GetFiles("*.xnb");

//            foreach (FileInfo file in files)
//            {
//                string fileName = TorqueUtil.ChopFileExtension(file.Name.Trim());
//                RegisterAsset(folderName + fileName);
//                Texture2D texture = ResourceManager.Instance.LoadTexture(folderName + fileName).Instance as Texture2D;
                
//                int textureSizeInMemory = texture.Width * texture.Height * 4;

//                // adjust estimated size depending on image format
//                //Debug.Write(" [" + texture.Format.ToString() + "] ");

//                switch (texture.Format)
//                {
//                    case SurfaceFormat.Dxt1:
//                        textureSizeInMemory /= 8;
//                        break;

//                    case SurfaceFormat.Dxt5:
//                        textureSizeInMemory /= 4;
//                        break;
//                }

//                //Debug.Write("Loaded texture: " + fileName);

//                //Debug.WriteLine(" => estimated memory allocation: " + string.Format("{0:0,0,0}", textureSizeInMemory));
//                _bytesLoaded += textureSizeInMemory;
//            }

//            // if requested then find all sub-folders and recurse into them to load any assets they contain
//            if (recursive)
//            {
//                DirectoryInfo[] folders = folderInfo.GetDirectories();

//                foreach (DirectoryInfo folder in folders)
//                {
//                    LoadTextures(folderName + folder.Name + @"/", recursive);
//                }
//            }
//        }

//        private void LoadFonts(string folderName, bool recursive)
//        {
//            //Debug.WriteLine("Loading FONTS from folder: " + folderName);

//            DirectoryInfo folderInfo = new DirectoryInfo(folderName);

//            // find all assets and load them
//            FileInfo[] files = folderInfo.GetFiles("*.xnb");

//            foreach (FileInfo file in files)
//            {
//                string fileName = TorqueUtil.ChopFileExtension(file.Name.Trim());
//                //Debug.WriteLine("Loading font: " + fileName);
//                RegisterAsset(folderName + fileName);
//                ResourceManager.Instance.LoadFont(folderName + fileName);
//            }

//            // if requested then find all sub-folders and recurse into them to load any assets they contain
//            if (recursive)
//            {
//                DirectoryInfo[] folders = folderInfo.GetDirectories();

//                foreach (DirectoryInfo folder in folders)
//                {
//                    LoadFonts(folderName + folder.Name + @"/", recursive);
//                }
//            }
//        }
//    }
//}
