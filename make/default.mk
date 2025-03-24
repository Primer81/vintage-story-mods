.DEFAULT_GOAL: all

.PHONY: all
all: project-build-all

.PHONY: clean
clean: project-build-clean

.PHONY: rebuild
rebuild:
	$(MAKE) clean
	$(MAKE) all

.PHONY: install
install: project-install-all

.PHONY: uninstall
uninstall: project-install-clean

.PHONY: run
run: project-run-client