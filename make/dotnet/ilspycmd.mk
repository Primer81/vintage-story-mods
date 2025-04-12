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
ifeq ($(words $(DOTNET_ILSPYCMD_TARGETS_INDEX)),1)
	$(warning Only one target detected. \
		Duplicating target to generate solution file.\
		The error 'System.ArgumentException: An item with the same \
		key has already been added. Key: \
		$(notdir $(basename $(lastword $(DOTNET_ILSPYCMD_TARGETS))))' \
		is expected and can be ignored.)
	$(eval DOTNET_ILSPYCMD_TARGETS=\
		$(DOTNET_ILSPYCMD_TARGETS) $(DOTNET_ILSPYCMD_TARGETS))
endif
	-$(DOTNET_ILSPYCMD) $(DOTNET_ILSPYCMD_TARGETS) $(DOTNET_ILSPYCMD_FLAGS)

.PHONY: dotnet-ilspycmd-clean
dotnet-ilspycmd-clean:
	rm -rf $(DOTNET_ILSPYCMD_OUTPUT_DIR)

.PHONY: dotnet-ilspycmd-rebuild
dotnet-ilspycmd-rebuild:
	$(MAKE) dotnet-ilspycmd-clean
	$(MAKE) dotnet-ilspycmd-all
