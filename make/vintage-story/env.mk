ifndef VINTAGE_STORY_DATA
    ifeq ($(OS),Windows_NT)
        ifndef APPDATA
            $(warning APPDATA is undefined. Needed to define VINTAGE_STORY_DATA)
        else
            export VINTAGE_STORY_DATA=$(APPDATA)/VintagestoryData
        endif
    else
        ifndef HOME
            $(error HOME is undefined. Needed to define VINTAGE_STORY_DATA)
        else
            export VINTAGE_STORY_DATA=$(HOME)/.config/VintagestoryData
        endif
    endif
endif
export VINTAGE_STORY_DATA:=$(subst \,/,$(VINTAGE_STORY_DATA))

ifndef VINTAGE_STORY
    ifeq ($(OS),Windows_NT)
        ifndef APPDATA
            $(error APPDATA is undefined. Needed to define VINTAGE_STORY)
        endif
        export VINTAGE_STORY=$(APPDATA)/Vintagestory
    else
        ifndef HOME
            $(error HOME is undefined. Needed to define VINTAGE_STORY)
        endif
        export VINTAGE_STORY=$(HOME)/.config/Vintagestory
    endif
endif
export VINTAGE_STORY:=$(subst \,/,$(VINTAGE_STORY))