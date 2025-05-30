###############################################################################
# Python
###############################################################################
export PYTHON?=python

###############################################################################
# Scripts
###############################################################################
SCRIPT_DIR=scripts
SCRIPT_UPDATE_INI_PY=$(SCRIPT_DIR)/update-ini.py
SCRIPT_SORT_JSON_PY=$(SCRIPT_DIR)/sort-json.py
SCRIPT_UPDATE_BOOKMARK_LABELS_PY=$(SCRIPT_DIR)/update-bookmark-labels.py

###############################################################################
# Common Definitions
###############################################################################
define newline


endef

###############################################################################
# Common Functions
###############################################################################
read_json=$(shell $(PYTHON) -c "import json; print(json.load(open('$(1)'))['$(2)'])" 2>/dev/null)
read_xml=$(shell $(PYTHON) -c "import xml.etree.ElementTree as ET; print(elem.text ET.parse('$(1)').getroot().find('.//$(2)').text)" 2>/dev/null)
define zip
    $(PYTHON) -c "import shutil; shutil.make_archive('$(if $(2),$(2),$(basename $(1)))', 'zip', '$(1)')"
endef
write_ini=$(PYTHON) $(SCRIPT_UPDATE_INI_PY) "$(1)" $(2) $(3) $(4)
sort_json_file=$(PYTHON) $(SCRIPT_SORT_JSON_PY) $(1)
update_bookmark_labels=$(PYTHON) $(SCRIPT_UPDATE_BOOKMARK_LABELS_PY) $(1)
lowercase=$(shell echo $(1) | tr A-Z a-z)
now=$(shell date +%Y%m%dT%H%M%S)
rwildcard=$(strip \
	$(wildcard $(1)/$(2)) \
	$(foreach item,$(wildcard $(1)/*),\
		$(if $(wildcard $(item)/*),\
			$(call rwildcard,$(item),$(2)),\
		)\
	))

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
# Backups
###############################################################################
BACKUP_TIMESTAMP:=$(call now)
BACKUP_DIR=backups

###############################################################################
# Dotnet
###############################################################################
## Configuration
export DOTNET?=dotnet
export DOTNET_BUILD?=dotnet build
export DOTNET_CLEAN?=dotnet clean
export DOTNET_RUN?=dotnet run
export DOTNET_PUBLISH?=dotnet publish

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
# Dotnet bepinex
###############################################################################
## Configuration (reference: https://github.com/BepInEx/BepInEx.Templates/blob/master/README.md)
export DOTNET_BEPINEX_SOURCE?=https://nuget.bepinex.dev/v3/index.json
export DOTNET_BEPINEX_PACKAGE_NAME?=BepInEx.Templates
## Definitions
DOTNET_BEPINEX=$(DOTNET) new bepinex5plugin
DOTNET_BEPINEX_INSTALL_SENTINEL=\
    $(SENTINEL_TMP_DIR)/dotnet-bepinex-install$(SENTINEL_EXT)
DOTNET_BEPINEX_CONFIG_RELPATH=BepInEx/config/BepInEx.cfg