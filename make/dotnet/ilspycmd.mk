.PHONY: dotnet-ilspycmd-uninstall
dotnet-ilspycmd-uninstall:
	$(DOTNET) tool uninstall $(DOTNET_ILSPYCMD_PACKAGE_NAME) -g
	rm -f $(DOTNET_ILSPYCMD_INSTALL_SENTINEL)

.PHONY: dotnet-ilspycmd-install
dotnet-ilspycmd-install: $(DOTNET_ILSPYCMD_INSTALL_SENTINEL)
$(DOTNET_ILSPYCMD_INSTALL_SENTINEL):
	$(shell mkdir -p $(dir $@))
	$(DOTNET) tool install $(DOTNET_ILSPYCMD_PACKAGE_NAME) -g \
		--version $(DOTNET_ILSPYCMD_PACKAGE_VERSION)
	touch $@

.PHONY: dotnet-ilspycmd-update
dotnet-ilspycmd-update:
	$(MAKE) dotnet-ilspycmd-uninstall
	$(MAKE) dotnet-ilspycmd-install

.PHONY: dotnet-ilspycmd-all
dotnet-ilspycmd-all: $(DOTNET_ILSPYCMD_DECOMPILE_PREREQUISITES)
	$(shell mkdir -p $(DOTNET_ILSPYCMD_OUTPUT_DIR))
	$(DOTNET_ILSPYCMD) $(DOTNET_ILSPYCMD_TARGETS) \
		--project\
		--disable-updatecheck\
		--nested-directories\
		--use-varnames-from-pdb\
		$(foreach ref,$(DOTNET_ILSPYCMD_REFERENCES),\
			-r $(ref)\
		)\
		-o $(DOTNET_ILSPYCMD_OUTPUT_DIR)

.PHONY: dotnet-ilspycmd-clean
dotnet-ilspycmd-clean:
	rm -rf $(DOTNET_ILSPYCMD_OUTPUT_DIR)

.PHONY: dotnet-ilspycmd-rebuild
dotnet-ilspycmd-rebuild:
	$(MAKE) dotnet-ilspycmd-clean
	$(MAKE) dotnet-ilspycmd-all
