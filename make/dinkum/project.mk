###############################################################################
# Create
###############################################################################
.PHONY: project-create-all
project-create-all: $(PROJECT_CREATE_SENTINEL)
$(PROJECT_CREATE_SENTINEL): $(PROJECT_CREATE_PREREQUISITES)
	$(shell mkdir -p $(dir $@))
	$(shell mkdir -p $(dir $(PROJECT_MOD_ICON)))
	$(eval $@_TemplateProjectName="\[{ProjectName}\]")
	$(eval $@_ActualProjectName="$(PROJECT_NAME)")
	$(eval $@_TemplateModId="\[{ProjectModId}\]")
	$(eval $@_ActualModId="$(PROJECT_MODID)")
	$(DOTNET_BEPINEX) \
		--output $(PROJECT_DIR)/$(PROJECT_NAME) \
		--name $(PROJECT_NAME)
	sed -i 's|$($@_TemplateProjectName)|$($@_ActualProjectName)|g' $(PROJECT_DIR)/$(notdir $(TEMPLATE_CAKE))/Program.cs
	cp $(TEMPLATE_MOD_SOLUTION_FILE) $(PROJECT_DIR)/$(PROJECT_NAME).sln
	$(DOTNET) sln $(PROJECT_DIR)/$(PROJECT_NAME).sln \
		add $(PROJECT_DIR)/$(PROJECT_NAME)/$(PROJECT_NAME).csproj
	cp $(TEMPLATE_MOD_INFO_JSON) $(PROJECT_SRC_DIR)
	cp -r $(TEMPLATE_ASSETS) $(PROJECT_SRC_DIR)
	sed -i 's|$($@_TemplateProjectName)|$($@_ActualProjectName)|g' $(PROJECT_DIR)/$(PROJECT_NAME)/$(notdir $(TEMPLATE_MOD_INFO_JSON))
	sed -i 's|$($@_TemplateModId)|$($@_ActualModId)|g' $(PROJECT_DIR)/$(PROJECT_NAME)/$(notdir $(TEMPLATE_MOD_INFO_JSON))
	cp $(TEMPLATE_GIT_IGNORE) $(PROJECT_DIR)
	cp $(PROJECT_MOD_ICON_DEFAULT) $(PROJECT_MOD_ICON)
	touch $@

.PHONY: project-create-clean
project-create-clean:
	rm -drf $(PROJECT_DIR)
	rm -f $(PROJECT_CREATE_SENTINEL)

.PHONY: project-create-rebuild
project-create-rebuild:
	$(MAKE) project-create-clean
	$(MAKE) project-create-all

###############################################################################
# Build
###############################################################################
.PHONY: project-build-all
project-build-all: $(PROJECT_BUILD_ALL_PREREQUISITES)

.PHONY: project-build-clean
project-build-clean: $(PROJECT_BUILD_CLEAN_PREREQUISITES)

###############################################################################
# Install
###############################################################################
.PHONY: project-install-all
project-install-all: $(PROJECT_INSTALL_PREREQUISITES)
	cp $(PROJECT_TARGET_RELEASE) $(VINTAGE_STORY)/Mods

.PHONY: project-install-clean
project-install-clean:
	rm -f $(VINTAGE_STORY)/Mods/$(PROJECT_MODID)*

###############################################################################
# Run
###############################################################################
.PHONY: project-run-client
project-run-client: $(PROJECT_RUN_PREREQUISITES)
	"$(VINTAGE_STORY)/Vintagestory" \
		--tracelog \
		--addModPath $(abspath $(PROJECT_SRC_DIR)/bin/Debug/Mods) \
		--openWorld "TestWorld"

# .PHONY: project-run-server
# project-run-server: $(PROJECT_RUN_PREREQUISITES)
# 	"$(VINTAGE_STORY)/VintagestoryServer" \
# 		--tracelog \
# 		--addModPath $(abspath $(PROJECT_SRC_DIR)/bin/Debug/Mods)

###############################################################################
# Targets
###############################################################################
## Mod
.PHONY: project-target-mod-all
project-target-mod-all: $(PROJECT_TARGET_PREREQUISITES)
	$(DOTNET_BUILD) -c Debug $(PROJECT_SRC_DIR)/$(PROJECT_NAME).csproj

.PHONY: project-target-mod-clean
project-target-mod-clean:
	$(DOTNET_CLEAN) -c Debug $(PROJECT_SRC_DIR)/$(PROJECT_NAME).csproj

.PHONY: project-target-mod-rebuild
project-target-mod-rebuild:
	$(MAKE) project-target-mod-clean
	$(MAKE) project-target-mod-all

## Release
.PHONY: project-target-release-all
project-target-release-all: $(PROJECT_TARGET_PREREQUISITES)
	$(eval $@_TemplateProjectName="\[{ProjectName}\]")
	$(eval $@_ActualProjectName="$(PROJECT_NAME)")
	$(eval $@_TemplateDescription="\[{ProjectDescription}\]")
	$(eval $@_ActualDescription="$(PROJECT_DESCRIPTION)")
	$(eval $@_TemplateVersion="\[{ProjectVersion}\]")
	$(eval $@_ActualVersion="$(PROJECT_VERSION)")
	$(eval $@_TemplateModId="\[{ProjectModId}\]")
	$(eval $@_ActualModId="$(PROJECT_MODID)")
	rm -rf $(PROJECT_RELEASES_DIR)
	mkdir -p $(PROJECT_RELEASES_DIR)/$(PROJECT_MODID)
	$(DOTNET_CLEAN) -c Release $(PROJECT_SRC_DIR)/$(PROJECT_NAME).csproj
	$(DOTNET_BUILD) -c Release $(PROJECT_SRC_DIR)/$(PROJECT_NAME).csproj
	$(DOTNET_PUBLISH) -c Release $(PROJECT_SRC_DIR)/$(PROJECT_NAME).csproj
	cp -r $(PROJECT_SRC_DIR)/bin/Release/net35/publish/* $(PROJECT_RELEASES_DIR)/$(PROJECT_MODID)
	$(if $(wildcard $(PROJECT_SRC_DIR)/assets),cp -r $(PROJECT_SRC_DIR)/assets $(PROJECT_RELEASES_DIR)/$(PROJECT_MODID),)
	$(if $(wildcard $(PROJECT_SRC_DIR)/modicon.png),cp $(PROJECT_SRC_DIR)/modicon.png $(PROJECT_RELEASES_DIR)/$(PROJECT_MODID),)
	cp $(TEMPLATE_MOD_INFO_JSON) $(PROJECT_RELEASES_DIR)/$(PROJECT_MODID)
	sed -i 's|$($@_TemplateProjectName)|$($@_ActualProjectName)|g' $(PROJECT_RELEASES_DIR)/$(PROJECT_MODID)/$(notdir $(TEMPLATE_MOD_INFO_JSON))
	sed -i 's|$($@_TemplateDescription)|$($@_ActualDescription)|g' $(PROJECT_RELEASES_DIR)/$(PROJECT_MODID)/$(notdir $(TEMPLATE_MOD_INFO_JSON))
	sed -i 's|$($@_TemplateVersion)|$($@_ActualVersion)|g' $(PROJECT_RELEASES_DIR)/$(PROJECT_MODID)/$(notdir $(TEMPLATE_MOD_INFO_JSON))
	sed -i 's|$($@_TemplateModId)|$($@_ActualModId)|g' $(PROJECT_RELEASES_DIR)/$(PROJECT_MODID)/$(notdir $(TEMPLATE_MOD_INFO_JSON))
	$(call zip,$(PROJECT_RELEASES_DIR)/$(PROJECT_MODID),$(PROJECT_RELEASES_DIR)/$(PROJECT_MODID)_$(PROJECT_VERSION))

.PHONY: project-target-release-clean
project-target-release-clean:
	$(DOTNET_CLEAN) -c Release $(PROJECT_SRC_DIR)/$(PROJECT_NAME).csproj
	rm -drf $(PROJECT_RELEASES_DIR)

.PHONY: project-target-release-rebuild
project-target-release-rebuild:
	$(MAKE) project-target-release-clean
	$(MAKE) project-target-release-all