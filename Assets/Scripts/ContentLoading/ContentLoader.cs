using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Content;
using System.Diagnostics;
using GarageGames.Torque.Core;
using System.Threading;
using System.IO;
using Microsoft.Xna.Framework.Graphics;
using GarageGames.Torque.Util;
using Microsoft.Xna.Framework.Media;



namespace MathFreak.ContentLoading
{
    /// <summary>
    /// We need to do some custom stuff with content loading - such as being able to query if an asset is
    /// loaded yet rather than the default behaviour which is to load and not query.
    /// 
    /// NOTE: Because of the way TX objects are created on deserialization and to keep our contentloader
    /// separate from the TX engine, the content loader will only load assets listed for loading - that is,
    /// the usual Load method will only delegate to the base class if the asset is in the registered-assets list.
    /// A RegisterAsset method is provided that adds an asset to the list.
    /// This means that when the TX resource manager tries to 'load' an asset then it will get null unless we
    /// explicitly registered the asset for loading (we are no longer letting TX load stuff on it's own
    /// schedule - we are taking full control of asset loading).  Modifications to the TX code to handle the nulll
    /// asset have been added to the engine in the appropriate places - search for "DBC - HANDLE NULL ASSET" to
    /// find the modifications.
    /// 
    /// NOTE: In addition to each loader's own list of the assets it has registered with it, there is also
    /// a shared asset list.  This means that when an asset is requested the caller does not need to know
    /// which loader loaded the asset (the asset's entry in the shared list has a reference to the loader
    /// that loaded it).  It also means there doesn't need to be duplicates of assets loaded - if a loader
    /// unloads it's assets then that is fine, because if another loader has the asset registered then the
    /// asset will be loaded again if it is requested - so, in short... the content that loaders load can
    /// overlap and the code requesting assets does not need to worry about it! =)
    /// </summary>
    public class ContentLoader : ContentManager
    {
        private static Dictionary<string, ContentLoader> _registeredSharedAssets = new Dictionary<string, ContentLoader>();
        private static List<ContentLoader> _allContentLoaders = new List<ContentLoader>();
        private static ContentLoader _proxyContentLoader;

        private Dictionary<string, AssetData> _registeredPrivateAssets = new Dictionary<string, AssetData>();
        private static int _textureBytesInMemory = 0;

        public static int TextureBytesInMemory
        {
            get { return _textureBytesInMemory; }
        }


        public ContentLoader(IServiceProvider sp)
            : base(sp)
        {
        }

        public ContentLoader(IServiceProvider sp, string rootDir)
            : base(sp, rootDir)
        {
            // Create the proxy content loader  (the proxy what now?  it's like this... the TX
            // resource manager stores content managers/loaders in a stack so we would not be
            // able to remove loaders arbitrarily; only pop the topmost one, which obviously
            // sucks - so instead we just put a single dummy loader on the stack that doesn't
            // have any assets registered, but is the gateway to the rest of our content
            // loading - since in our system the content loaders actually talk to each other
            // we only need to put the one 'proxy' content loader on the resource manager's
            // stack anyway =)
            if (_proxyContentLoader == null)
            {
                _proxyContentLoader = this; // temporarily we need to set this variable to something lest creating the proxy loader lead to the proxy loader trying to create a proxy loader... ad infinitum
                _proxyContentLoader = new ContentLoader(sp, rootDir);
                ResourceManager.Instance.PushContentManager(_proxyContentLoader);
            }

            // add ourselves to the list that *really* matters ;-)
            _allContentLoaders.Add(this);
        }

        public override T Load<T>(string assetName)
        {
            // if the asset is registered globally then load it
            if (_registeredSharedAssets.ContainsKey(assetName))
            {
                //Debug.WriteLine("....Asset is globally registered: " + assetName);
                return _registeredSharedAssets[assetName].LoadAsset<T>(assetName);
            }
            // else if we can successfully register it as a global asset (will be successful if any of the active content loaders have the asset registered)
            // then load it
            else if (RegisterSharedAsset(assetName))
            {
                //Debug.WriteLine("....Asset is PRIVATELY registered: " + assetName);
                return Load<T>(assetName);
            }
            // else it can't be loaded
            else
            {
                //Debug.WriteLine("....Asset load request ignored: " + assetName);
                return default(T);
            }
        }

        private T LoadAsset<T>(string assetName)
        {
            try
            {
                T asset = base.Load<T>(assetName);

                if (_registeredPrivateAssets[assetName].AssetRef == null)
                {
                    _registeredPrivateAssets[assetName].AssetRef = asset;    // keeps track of what assets are loaded - useful for debugging or tracking texture memory usage and things like that

#if DEBUG
                    TrackTextureMemoryAdded(_registeredPrivateAssets[assetName]);
#endif
                }

                return asset;
            }
            catch (Exception e)
            {
                Assert.Fatal(false, "Failed to load asset: " + assetName);
                Assert.Fatal(false, "....Exception: \n" + e.Message);
                return default(T);
            }
        }

        private void TrackTextureMemoryAdded(AssetData data)
        {
            Texture2D texture = data.AssetRef as Texture2D;

            if (texture != null)
            {
                int textureSizeInMemory = texture.Width * texture.Height * 4;

                // adjust estimated size depending on texture format
                switch (texture.Format)
                {
                    case SurfaceFormat.Dxt1:
                        textureSizeInMemory /= 8;
                        break;

                    case SurfaceFormat.Dxt5:
                        textureSizeInMemory /= 4;
                        break;

                    // else assume 24bit (TODO: include other formats if needed)
                }

                _textureBytesInMemory += textureSizeInMemory;
                data.TextureBytesInMemory = textureSizeInMemory;

                //Debug.WriteLine("Loaded texture size: " + textureSizeInMemory);
                //Debug.WriteLine("Total texture memory: " + TextureBytesInMemory);
            }
        }

        private void TrackTextureMemoryRemovedAll()
        {
            foreach (AssetData data in _registeredPrivateAssets.Values)
            {
                _textureBytesInMemory -= data.TextureBytesInMemory;

                //Debug.WriteLine("Removed texture size: " + data.TextureBytesInMemory);
                //Debug.WriteLine("Total texture memory: " + TextureBytesInMemory);
            }
        }

        public T LoadAssetDirectly<T>(string assetName)
        {
            if (_registeredPrivateAssets.ContainsKey(assetName))
            {
                return LoadAsset<T>(assetName);
            }
            else
            {
                Assert.Fatal(false, "Trying to directly load an asset that hasn't been registered with the content loader - assetname is: " + assetName);
                return default(T);
            }
        }

        // Does not return the asset that was loaded - this method is purely for preloading assets.
        // Override this method to enable preloading assets other than the default types provided
        // by XNA.
        protected virtual void PreLoadAsset(Type assetType, string assetName)
        {
            // NOTE: in order of most likely to be used
            switch (assetType.Name)
            {
                case "Texture2D":
                    LoadAsset<Texture2D>(assetName);
                    break;

                case "Effect":
                    LoadAsset<Effect>(assetName);
                    break;

                case "SpriteFont":
                    LoadAsset<SpriteFont>(assetName);
                    break;

                case "Texture":
                    LoadAsset<Texture>(assetName);
                    break;

                case "Model":
                    LoadAsset<Model>(assetName);
                    break;

                case "Texture3D":
                    LoadAsset<Texture3D>(assetName);
                    break;

                case "TextureCube":
                    LoadAsset<TextureCube>(assetName);
                    break;

                case "Video":
                    LoadAsset<Video>(assetName);
                    break;

                default:
                    Assert.Fatal(false, "ContentLoader does not know how to load asset type: " + assetType.Name);
                    break;
            }
        }

        public void RegisterAsset(Type assetType, string assetName)
        {
            // register the asset in our private list
            if (!_registeredPrivateAssets.ContainsKey(assetName))
            {
                //Debug.WriteLine("Registering my Asset: " + assetName);
                _registeredPrivateAssets.Add(assetName, new AssetData() { AssetType = assetType, AssetRef = null });
            }
        }

        public void RegisterAssets(Type assetType, string path, bool recursive, ContentBlock.ProgressMeter progress)
        {
            DirectoryInfo folderInfo = new DirectoryInfo(path);

            // find all assets and register them
            FileInfo[] files = folderInfo.GetFiles("*.xnb");

            foreach (FileInfo file in files)
            {
                string fileName = TorqueUtil.ChopFileExtension(file.Name.Trim());
                RegisterAsset(assetType, path + fileName);

                if (progress != null)
                {
                    progress.ObjectCount++;
                }
            }

            // if requested then find all sub-folders and recurse
            if (recursive)
            {
                DirectoryInfo[] folders = folderInfo.GetDirectories();

                foreach (DirectoryInfo folder in folders)
                {
                    RegisterAssets(assetType, path + folder.Name + @"/", recursive, progress);
                }
            }
        }

        /// <summary>
        /// Returns true if the asset was successfully registered in the global list - i.e. any one of the
        /// active content loaders had the asset registered privately and thus the asset was eligible
        /// for registering globally.
        /// 
        /// NOTE: this should really only be called just before the asset is actually loaded - it will be assumed that any asset in the shared list has been loaded
        /// </summary>
        private static bool RegisterSharedAsset(string assetName)
        {
            Assert.Fatal(!_registeredSharedAssets.ContainsKey(assetName), "RegisterSharedAsset() - the named asset is already registered - asset name: " + assetName);

            foreach (ContentLoader loader in _allContentLoaders)
            {
                if (loader._registeredPrivateAssets.ContainsKey(assetName))
                {
                    _registeredSharedAssets.Add(assetName, loader);
                    return true;
                }
            }

            return false;
        }

        // Note: dispose doesn't do anything at the moment as Unload() will be called anyway and we will do any cleaning up there
        protected override void Dispose(bool disposing)
        {
            _allContentLoaders.Remove(this);
            base.Dispose(disposing);
        }

        // Could use 'new' keyword but we don't own the underlying implementation and don't want to mess it up at all
        public void DisposeAll()
        {
            Dispose(true);
        }

        public override void Unload()
        {
#if DEBUG
            TrackTextureMemoryRemovedAll();
#endif

            base.Unload();
            UnregisterAllAssets();
        }

        public void UnregisterAllAssets()
        {
            // unregister all our assets from the shared list
            foreach (string assetName in _registeredPrivateAssets.Keys)
            {
                // only remove assets that *we* loaded - don't remove assets that other loaders loaded!
                if (_registeredSharedAssets.ContainsKey(assetName) && _registeredSharedAssets[assetName] == this)
                {
                    //Debug.WriteLine("Unregistering from the global asset list: " + entry.Key);
                    _registeredSharedAssets.Remove(assetName);

                    //object asset = _registeredPrivateAssets[assetName];   // track assets
                    _registeredPrivateAssets[assetName].AssetRef = null; // help gc
                }
#if DEBUG
                //else
                //{
                //    if (!_registeredSharedAssets.ContainsKey(entry.Key))
                //    {
                //        Debug.WriteLine("Not unregistered from the global asset list (we didn't load it): " + entry.Key);
                //    }
                //    else
                //    {
                //        Debug.WriteLine("Not unregistered from the global asset list (it isn't in the list anyway): " + entry.Key);
                //    }
                //}
#endif
            }

            // clear our private list
            _registeredPrivateAssets.Clear();
        }

        public void UnregisterAsset(string assetName)
        {
            // only remove assets that *we* loaded - don't remove stuff other loaders loaded!
            if (_registeredSharedAssets.ContainsKey(assetName) && _registeredSharedAssets[assetName] == this)
            {
                _registeredSharedAssets.Remove(assetName);
            }

            // remove from private list
            //object asset = _registeredPrivateAssets[assetName];   // track assets
            _registeredPrivateAssets[assetName] = null; // help gc
            _registeredPrivateAssets.Remove(assetName);
        }

        // Loads all of this content loader's registered assets immediately, without waiting
        // for them to be requested.
        public void PreLoadRegisteredAssets(ContentBlock.ProgressMeter progress)
        {
            foreach (KeyValuePair<string, AssetData> assetKV in _registeredPrivateAssets)
            {
                // if the asset is not already in the shared list then add it
                if (!_registeredSharedAssets.ContainsKey(assetKV.Key))
                {
                    _registeredSharedAssets.Add(assetKV.Key, this); // register the asset in the shared list - we do this directly as no need to search all the content loaders; we know we are the one registering it
                }

                // actually load the asset - note: we will have our own copy of the asset regardless of whether another loader already loaded the same asset and registered it in the shared list (this is by design!)
                PreLoadAsset(assetKV.Value.AssetType, assetKV.Key);

                if (progress != null)
                {
                    progress.ObjectCount++;
                    progress.TextureBytesInMemory += assetKV.Value.TextureBytesInMemory;
                }
            }
        }



        // Used by the content loader to store information on each asset - including a reference to the asset itself
        protected class AssetData
        {
            public Type AssetType;
            public object AssetRef;
            public int TextureBytesInMemory;    // currently only valid for Texture2D
        }
    }
}
