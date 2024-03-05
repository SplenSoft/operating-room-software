# Change Log
All notable changes to this project will be documented in this file.

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