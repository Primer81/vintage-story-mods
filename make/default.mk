.PHONY: all
all: project-build-all

.PHONY: clean
clean: project-build-clean

.PHONY: rebuild
rebuild:
	$(MAKE) clean
	$(MAKE) all

.PHONY: run
run: project-run-client