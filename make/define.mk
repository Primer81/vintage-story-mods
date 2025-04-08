###############################################################################
# Common Definitions
###############################################################################
define newline


endef

###############################################################################
# Common Functions
###############################################################################
read_json=$(shell python -c "import json; print(json.load(open('$(1)'))['$(2)'])" 2>/dev/null)

###############################################################################
# Images
###############################################################################
IMG_DIR=img
IMG_PROFILE_PICTURE=$(IMG_DIR)/Misc/ProfilePicture.png

###############################################################################
# Sentinels
###############################################################################
SENTINEL_DIR=sentinels
SENTINEL_TMP_DIR=$(SENTINEL_DIR)/tmp
SENTINEL_EXT=.sentinel

###############################################################################
# Dotnet vsmod
###############################################################################
## Configuration
export DOTNET?=dotnet
export DOTNET_VSMOD_PACKAGE_NAME?=VintageStory.Mod.Templates
## Definitions
DOTNET_VSMOD=$(DOTNET) new vsmod
DOTNET_VSMOD_INSTALL_SENTINEL=\
    $(SENTINEL_TMP_DIR)/dotnet-vsmod-install$(SENTINEL_EXT)

###############################################################################
# Dotnet ilspycmd
###############################################################################
## Configuration
export DOTNET_ILSPYCMD_PACKAGE_NAME?=ilspycmd
export DOTNET_ILSPYCMD_PACKAGE_VERSION?=8.2
export DOTNET_ILSPYCMD_TARGETS?=\
	$(VINTAGE_STORY)/VintagestoryAPI.dll\
	$(VINTAGE_STORY)/VintagestoryLib.dll\
	$(VINTAGE_STORY)/Mods/VSEssentials.dll\
	$(VINTAGE_STORY)/Mods/VSSurvivalMod.dll\
	$(VINTAGE_STORY)/Mods/VSCreativeMod.dll
export DOTNET_ILSPYCMD_REFERENCES?=\
	$(VINTAGE_STORY)\
	$(VINTAGE_STORY)/Mods\
	$(VINTAGE_STORY)/Lib
export DOTNET_ILSPYCMD_VSVERSION?=new
## Definitions
### Commands
DOTNET_ILSPYCMD=ilspycmd
DOTNET_ILSPYCMD_INSTALL_SENTINEL=\
    $(SENTINEL_TMP_DIR)/dotnet-ilspycmd-install$(SENTINEL_EXT)
### Directories
DOTNET_ILSPYCMD_OUTPUT_DIR=references/$(DOTNET_ILSPYCMD_VSVERSION)/DecompiledSource
### Recipes
DOTNET_ILSPYCMD_DECOMPILE_PREREQUISITES=\
	| $(DOTNET_ILSPYCMD_INSTALL_SENTINEL)

###############################################################################
# Project
###############################################################################
## Sources
SRC_DIR=src
## Configuration
ifndef name
    ### Defaults
    export PROJECT_NAME_LIST?=$(patsubst $(SRC_DIR)/%,%,$(wildcard $(SRC_DIR)/*))
    export PROJECT_NAME?=DataDumper
else
    ### Shorthand
    export PROJECT_NAME_LIST?=$(name)
    export PROJECT_NAME?=$(name)
endif
## Definitions
### Directories
#### Common
PROJECT_DIR=$(SRC_DIR)/$(PROJECT_NAME)
PROJECT_RELEASES_DIR=$(PROJECT_DIR)/Releases
#### Cake
PROJECT_CAKE_SRC_DIR=$(PROJECT_DIR)/CakeBuild
PROJECT_CAKE_BUILD_DIR=\
    $(PROJECT_CAKE_BIN_DIR)/$(PROJECT_BUILD_PROFILE)/net7.0
PROJECT_CAKE_CSPROJ_FILE=$(PROJECT_SRC_DIR)/CakeBuild.csproj
#### Mod
PROJECT_SRC_DIR=$(PROJECT_DIR)/$(PROJECT_NAME)
PROJECT_BUILD_DIR=\
    $(PROJECT_SRC_DIR)/bin/$(PROJECT_BUILD_PROFILE)/Mods/mod
PROJECT_CSPROJ_FILE=$(PROJECT_SRC_DIR)/$(PROJECT_NAME).csproj
### Version
PROJECT_VERSION=$(call read_json,$(PROJECT_SRC_DIR)/modinfo.json,version)
PROJECT_MODID=$(call read_json,$(PROJECT_SRC_DIR)/modinfo.json,modid)
ifeq ($(PROJECT_VERSION),)
    PROJECT_VERSION=1.0.0
endif
ifeq ($(PROJECT_MODID),)
    PROJECT_MODID=unknown
endif
### Icon
PROJECT_MOD_ICON_DEFAULT=$(IMG_PROFILE_PICTURE)
PROJECT_MOD_ICON=$(PROJECT_SRC_DIR)/modicon.png
### Recipes
#### Create
PROJECT_CREATE_SENTINEL=\
    $(SENTINEL_DIR)/project-create-$(PROJECT_NAME)$(SENTINEL_EXT)
PROJECT_CREATE_PREREQUISITES=\
    | $(DOTNET_VSMOD_INSTALL_SENTINEL)
#### Build
PROJECT_BUILD_ALL_PREREQUISITES=\
    project-target-cake-all\
    project-target-mod-all\
    project-target-release-all
PROJECT_BUILD_CLEAN_PREREQUISITES=\
    project-target-cake-clean\
    project-target-mod-clean\
    project-target-release-clean
#### Install
PROJECT_INSTALL_PREREQUISITES=\
    project-target-release-all
#### Run
PROJECT_RUN_PREREQUISITES=\
    project-build-all
#### Target
PROJECT_TARGET_PREREQUISITES=\
    | $(PROJECT_CREATE_SENTINEL)
PROJECT_TARGET_RELEASE=\
    $(PROJECT_RELEASES_DIR)/$(PROJECT_MODID)_$(PROJECT_VERSION).zip