###############################################################################
# Python
###############################################################################
export PYTHON?=python
PYTHON_CHECK := $(shell which $(PYTHON) 2>/dev/null)
$(if $(PYTHON_CHECK),,$(error '$(PYTHON)' is not installed. Please install '$(PYTHON)' before continuing.))