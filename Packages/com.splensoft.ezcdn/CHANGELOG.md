# Change Log
All notable changes to this project will be documented in this file.

## [1.2.0] - 2024-03-07

### Added
- IPreprocessAssetBundle interface added, following existing semantics in Unity. Implements OnPreprocessAssetBundle, which allows custom processing before the asset bundle is packaged and uploaded by EZ-CDN. IPreprocessAssetBundle should only be added to root-level components on prefabs or to ScriptableObjects that are managed by EZ-CDN (ManagedAsset attribute or added in Settings)
- Verbose option now available as a Log Level. Default is still Log
- AutoInstantiator ScriptableObject, which will automatically instantiate prefab asset bundles on app start
- MaxConcurrentDownloads option in settings
- Global events in AssetBundleManager - AssetRetrievalStarted, AssetLoaded, AssetBundleDownloadStarted, AssetBundleDownloadFinished, SceneAssetRetrievalStarted, SceneAssetLoaded. These are UnityEvents and will pass the asset bundle name as the argument

### Fixes
- Fix GUILayout issue with AssetBundleReferenceAttribute
- Add missing hookup to SceneRequester to download and load scene via an inspector event
- Change "Regenerate Asset Bundle Names" menu item to "Dry Run"

## [1.1.0] - 2024-03-04

### Added
- "Include Local Copy" option added in settings. Will include a new build of asset bundles in Streaming Assets when the app is built, and is used as a fall back when the app fails to get the asset bundle from the internet

## [1.0.1] - 2024-02-29
 
### Fixes
- Fix compatibility with 2021 versions of Unity
- Fix a build-related problem
 
## [1.0.0] - 2024-02-20
 
Initial release
 
### Added
- Connect to Unity Cloud Content Delivery with the absolute minimum setup required
- Content buckets are automatically generated and maintained by the plugin
- Add download-on-demand functionality to your custom fields with the AssetBundleReference attribute
- Automatically name, package and deploy assetbundles
- Works out of the box for most situations, but allows granular settings for advanced cases