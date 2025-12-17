# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.1.0](https://github.com/Slatyo/Valheim-Prime/compare/v1.0.0...v1.1.0) (2025-12-17)


### Features

* **abilities:** add default ability definitions ([cb8769d](https://github.com/Slatyo/Valheim-Prime/commit/cb8769dcf08d8e00639ea8985ba3d9e139848cc7))
* add elemental damage stats and spirit resistance ([5dba14c](https://github.com/Slatyo/Valheim-Prime/commit/5dba14c8637b7466f9912ef85a2e59a227841416))
* add vanilla base syncing and passive health regen ([35f5b1b](https://github.com/Slatyo/Valheim-Prime/commit/35f5b1b2f1e9d9fffb209440719c8d279110ee76))
* **commands:** add Munin command integration ([8c0e68a](https://github.com/Slatyo/Valheim-Prime/commit/8c0e68acddfbd16e748b9d6745c111fb0c7438d6))
* **i18n:** add German translations for all stats ([fa9b67d](https://github.com/Slatyo/Valheim-Prime/commit/fa9b67dbba0b28df84da72fd36a117726ec4f76d))
* initial release of Prime mod ([3cb3fd7](https://github.com/Slatyo/Valheim-Prime/commit/3cb3fd7d7f32bd409b655570e26dc4030b3c5ffe))
* **patches:** add stat patches for vanilla integration ([e44f9ab](https://github.com/Slatyo/Valheim-Prime/commit/e44f9ab8dad68d127c1dc098cc227502022693b6))
* **stats:** expand stat registry with combat and utility stats ([1f1960e](https://github.com/Slatyo/Valheim-Prime/commit/1f1960ed243e31b596d55bed1cf496f1c05d9374))


### Bug Fixes

* **combat:** improve damage calculation and add debug logging ([904b789](https://github.com/Slatyo/Valheim-Prime/commit/904b7895c3176de13c630697f0e7256e188f2805))
* **stats:** remove SetBase for resource stats to prevent double-counting ([1533aee](https://github.com/Slatyo/Valheim-Prime/commit/1533aeee843f0c373806b01fd7b3f8c0a6c40116))
* **stats:** sync max stamina/eitr fields before UpdateStats runs ([00f0feb](https://github.com/Slatyo/Valheim-Prime/commit/00f0feb86de8cc69e817e189f9359c0a19d3539a))


### Code Refactoring

* **stats:** use getter postfixes instead of field manipulation ([10e9c13](https://github.com/Slatyo/Valheim-Prime/commit/10e9c1370a370f9fd2a387577bbf773c6100a341))
* unify plugin GUID and add XML documentation ([bd473c6](https://github.com/Slatyo/Valheim-Prime/commit/bd473c6a4506e77347a8119cf0542bbcaa0a42e6))

## [1.0.0] - 2025-12-01

### Added
- Stat system with modifiers (flat, percent, multiply, override)
- Timed buffs/debuffs with automatic expiration
- Stacking modifiers with configurable behaviors
- Ability system with cooldowns, costs, and scaling
- Damage pipeline with crit, armor, and resistances
- Effect system with proc triggers (on-hit, on-damage-taken, etc.)
- Formula override system for mod customization
- Events for stat changes, combat, and abilities
- Console commands for testing and debugging
