# [1.8.0](https://github.com/dvos-tools/eventbus/compare/v1.7.0...v1.8.0) (2025-10-23)


### Features

* cleanup logic ([268273a](https://github.com/dvos-tools/eventbus/commit/268273af5057e2252ea5ff729410bbbe1ecbb52d))

# [1.7.0](https://github.com/dvos-tools/eventbus/compare/v1.6.0...v1.7.0) (2025-10-18)


### Features

* refactoring to use Service Layer logic ([5e8758e](https://github.com/dvos-tools/eventbus/commit/5e8758ebeacb727383f58aec224117af31bd9883))

# [1.6.0](https://github.com/dvos-tools/eventbus/compare/v1.5.0...v1.6.0) (2025-10-17)


### Features

* Forgot add  EventBusCore.RegisterStaticHandle as the depricated static handler... whoeps ([b6b8e1c](https://github.com/dvos-tools/eventbus/commit/b6b8e1c228687313a317096eb42313d0d11f1232))

# [1.5.0](https://github.com/dvos-tools/eventbus/compare/v1.4.0...v1.5.0) (2025-10-16)


### Features

* added api layer fixes ([6545fe1](https://github.com/dvos-tools/eventbus/commit/6545fe1ba99a63c10f3de20d1e36d56a0b1faf29))
* added api layer so you can call the EventBus staticly ([a7109f7](https://github.com/dvos-tools/eventbus/commit/a7109f7172af72e48ec14ddb7933b2c5c78746a4))

# [1.4.0](https://github.com/dvos-tools/eventbus/compare/v1.3.0...v1.4.0) (2025-10-12)


### Features

* removed package-lock.json and package-lock.json.meta ([3b2c053](https://github.com/dvos-tools/eventbus/commit/3b2c05379689d57a7fd73d2f91dcff579b9be7cb))

# [1.3.0](https://github.com/dvos-tools/eventbus/compare/v1.2.0...v1.3.0) (2025-10-01)


### Features

* buffered events with tests ([7260b2b](https://github.com/dvos-tools/eventbus/commit/7260b2b2d1ac9f7c0379ea9af02cc2155cdc649f))
* now with buffering, working sendAndWait, runtime AggregateReady and improved logging with Tests ([b7eca88](https://github.com/dvos-tools/eventbus/commit/b7eca88f0a73a43be3ebf0229d32104d2a1d2c7a))

# [1.2.0](https://github.com/dvos-tools/eventbus/compare/v1.1.0...v1.2.0) (2025-09-29)


### Features

* fixing namespaces ([cf33554](https://github.com/dvos-tools/eventbus/commit/cf335540a9651ff2af5edeec266055b5ecb679ba))

# [1.1.0](https://github.com/dvos-tools/eventbus/compare/v1.0.2...v1.1.0) (2025-09-23)


### Features

* actually from the main thread now ([b80b42e](https://github.com/dvos-tools/eventbus/commit/b80b42ec8f7b0ab7d8ae314e5629abbba6a7e976))

## [1.0.2](https://github.com/dvos-tools/eventbus/compare/v1.0.1...v1.0.2) (2025-09-23)


### Bug Fixes

* actual uuid for asmef ([9354ef4](https://github.com/dvos-tools/eventbus/commit/9354ef4ac70be79fb8ca32508ad1c113d8b56acb))
* add missing asmdef.meta file for Unity package compatibility ([27aed95](https://github.com/dvos-tools/eventbus/commit/27aed95e73170501d390ec78f9da7d7d095da126))

## [1.0.1](https://github.com/dvos-tools/eventbus/compare/v1.0.0...v1.0.1) (2025-09-23)


### Bug Fixes

* hopefully last try... ([5b7dcb7](https://github.com/dvos-tools/eventbus/commit/5b7dcb7af87b950c7e35088364709f05dec109c7))
* oke now without a release lol ([e252d73](https://github.com/dvos-tools/eventbus/commit/e252d7390db114898ffb5004b2d0320aa4f01cd0))

## 1.0.0 (2025-09-23)


### Features

* add comprehensive version management system ([0c2470f](https://github.com/dvos-tools/eventbus/commit/0c2470fcc86b83ee495627ace3b1d9f43d6d98fa))
* now with version history and release workflow.. I hope ([a6fcd6e](https://github.com/dvos-tools/eventbus/commit/a6fcd6e7574fe5131c80374fabc1d737632f004a))


### Bug Fixes

* try again... ([ae2b50a](https://github.com/dvos-tools/eventbus/commit/ae2b50a0d79254b634aded54e8db326efc6b6584))
* with gh permissions ([0077f9e](https://github.com/dvos-tools/eventbus/commit/0077f9ef72853b280618fe2696be06e4a807ab9f))

# Changelog

## [1.0.0] - 2025-01-27

### Added
- Event bus system for Unity 6
- Three dispatchers: Unity, ThreadPool, Immediate
- Custom dispatcher support
- Async/sync event sending
- Static handler registration
