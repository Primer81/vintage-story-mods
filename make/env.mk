PYTHON_CHECK := $(shell which python3 2>/dev/null || which python 2>/dev/null)
$(if $(PYTHON_CHECK),,$(error Python is not installed. Please install Python before continuing.))