.DEFAULT_GOAL: all

.PHONY: all
all:
	$(foreach project_name,$(PROJECT_NAME_LIST),\
		$(MAKE) PROJECT_NAME=$(project_name) project-build-all $(newline)\
	)

.PHONY: clean
clean:
	$(foreach project_name,$(PROJECT_NAME_LIST),\
		$(MAKE) PROJECT_NAME=$(project_name) project-build-clean $(newline)\
	)

.PHONY: rebuild
rebuild:
	$(MAKE) clean
	$(MAKE) all

.PHONY: install
install:
	$(MAKE) uninstall
	$(foreach project_name,$(PROJECT_NAME_LIST),\
		$(MAKE) PROJECT_NAME=$(project_name) project-install-all $(newline)\
	)

.PHONY: uninstall
uninstall:
	$(foreach project_name,$(PROJECT_NAME_LIST),\
		$(MAKE) PROJECT_NAME=$(project_name) project-install-clean $(newline)\
	)

.PHONY: run
run: project-run-client

.PHONY: decompile
decompile: dotnet-ilspycmd-rebuild

.PHONY: sort
sort:
	$(foreach project_bookmarks,$(call rwildcard,.,.vscode/bookmarks.json),\
		$(call sort_json_file,--sort-object-keys -r -k path -x "[^/\\\\\\\\]*$$" "$(project_bookmarks)") $(newline)\
	)