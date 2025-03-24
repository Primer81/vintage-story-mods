# Functions
read_json=$(shell python -c "import json; print(json.load(open('$(1)'))['$(2)'])" 2>/dev/null)

# Sentinels
SENTINEL_DIR=tmp
SENTINEL_EXT=.sentinel

# Dotnet
## Configuration
DOTNET?=dotnet
DOTNET_VSMOD_PACKAGE_NAME?=VintageStory.Mod.Templates
## Definitions
DOTNET_VSMODE=$(DOTNET) new vsmod
DOTNET_VSMOD_INSTALL_SENTINEL=\
    $(SENTINEL_DIR)/dotnet-vsmod-install$(SENTINEL_EXT)

# Project
## Configuration
PROJECT_NAME?=HotbarScrollControl
PROJECT_BUILD_PROFILE?=Debug
## Definitions
### Directories
PROJECT_DIR=src/$(PROJECT_NAME)
PROJECT_RELEASES_DIR=$(PROJECT_DIR)/Releases
PROJECT_SRC_DIR=$(PROJECT_DIR)/$(PROJECT_NAME)
PROJECT_BUILD_DIR=\
    $(PROJECT_SRC_DIR)/bin/$(PROJECT_BUILD_PROFILE)/net7.0
PROJECT_CAKE_SRC_DIR=$(PROJECT_DIR)/CakeBuild
PROJECT_CAKE_BUILD_DIR=\
    $(PROJECT_CAKE_SRC_DIR)/bin/$(PROJECT_BUILD_PROFILE)/net7.0
### Version
PROJECT_VERSION=$(call read_json,$(PROJECT_SRC_DIR)/modinfo.json,version)
### Recipes
PROJECT_CREATE_SENTINEL=\
    $(SENTINEL_DIR)/project-create-$(PROJECT_NAME)$(SENTINEL_EXT)
PROJECT_CREATE_PREREQUISITES=\
    $(DOTNET_VSMOD_INSTALL_SENTINEL)
PROJECT_BUILD_PREREQUISITES=\
    $(PROJECT_CREATE_SENTINEL)
PROJECT_RUN_PREREQUISITES=\
    project-build-all
