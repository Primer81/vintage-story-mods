ifndef DINKUM_SAVES
    ifeq ($(OS),Windows_NT)
        ifndef APPDATA
            $(warning APPDATA is undefined. Needed to define DINKUM_SAVES)
        else
            export DINKUM_SAVES=$(APPDATA)/../LocalLow/James Bendon/Dinkum
        endif
    else
        ifndef HOME
            $(warning HOME is undefined. Needed to define DINKUM_SAVES)
        else
            export DINKUM_SAVES=$(HOME)/.local/share/Steam/steamapps/compatdata/1062520/pfx/drive_c/users/steamuser/AppData/LocalLow/James Bendon
        endif
    endif
endif
export DINKUM_SAVES:=$(subst \,/,$(DINKUM_SAVES))

ifndef DINKUM_INSTALL
    ifeq ($(OS),Windows_NT)
        PROGRAMFILES_X86:=$(shell powershell -NoProfile -Command '$${env:ProgramFiles(x86)}')
        ifeq ($(PROGRAMFILES_X86),)
            $(error PROGRAMFILES_X86 is undefined. Needed to define DINKUM_INSTALL)
        endif
        export STEAM_PATH=$(PROGRAMFILES_X86)/Steam
        export STEAM_COMMON=$(STEAM_PATH)/steamapps/common
    else
        ifeq ($(HOME),)
            $(error HOME is undefined. Needed to define DINKUM_INSTALL)
        endif
        UNAME_S=$(shell uname -s)
        ifeq ($(UNAME_S),Darwin)
            # macOS
            export STEAM_PATH=$(HOME)/Library/Application\ Support/Steam
            export STEAM_COMMON=$(STEAM_PATH)/steamapps/common
        else
            # Linux
            export STEAM_PATH=$(HOME)/.local/share/Steam
            export STEAM_COMMON=$(STEAM_PATH)/steamapps/common
        endif
    endif
    export DINKUM_INSTALL=$(STEAM_COMMON)/Dinkum
endif
export DINKUM_INSTALL:=$(subst \,/,$(DINKUM_INSTALL))