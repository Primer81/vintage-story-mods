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
        export VINTAGE_STORY=$(HOME)/ApplicationData/vintagestory
    endif
endif
export VINTAGE_STORY:=$(subst \,/,$(VINTAGE_STORY))

PYTHON_CHECK := $(shell which python3 2>/dev/null || which python 2>/dev/null)
$(if $(PYTHON_CHECK),,$(error Python is not installed. Please install Python before continuing.))