###############################################################################
# Create
###############################################################################
.PHONY: project-create-all
project-create-all: $(PROJECT_CREATE_SENTINEL)
$(PROJECT_CREATE_SENTINEL): $(PROJECT_CREATE_PREREQUISITES)
	$(shell mkdir -p $(dir $@))
	$(DOTNET_VSMODE) \
		--AddSolutionFile \
		--IncludeVSCode \
		--AddSampleCode \
		--AddAssetFolder \
		--output $(PROJECT_DIR) \
		--name $(PROJECT_NAME)
	touch $@

.PHONY: project-create-clean
project-create-clean:
	rm -drf $(PROJECT_DIR)
	rm -f $(PROJECT_CREATE_SENTINEL)

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
	rm -f $(VINTAGE_STORY)/Mods/$(notdir $(PROJECT_TARGET_RELEASE))

###############################################################################
# Run
###############################################################################
.PHONY: project-run-client
project-run-client: $(PROJECT_RUN_PREREQUISITES)
	"$(VINTAGE_STORY)/Vintagestory" \
		--tracelog \
		--addModPath $(abspath $(PROJECT_SRC_DIR)/bin/Debug/Mods) \
		--openWorld "TestWorld"

.PHONY: project-run-server
project-run-server: $(PROJECT_RUN_PREREQUISITES)
	"$(VINTAGE_STORY)/VintagestoryServer" \
		--tracelog \
		--addModPath $(abspath $(PROJECT_SRC_DIR)/bin/Debug/Mods)

###############################################################################
# Targets
###############################################################################
## Cake
.PHONY: project-target-cake-all
project-target-cake-all: $(PROJECT_TARGET_PREREQUISITES)
	$(DOTNET) build -c Debug $(PROJECT_CAKE_SRC_DIR)/CakeBuild.csproj

.PHONY: project-target-cake-clean
project-target-cake-clean:
	$(DOTNET) clean -c Debug $(PROJECT_CAKE_SRC_DIR)/CakeBuild.csproj

## Mod
.PHONY: project-target-mod-all
project-target-mod-all: $(PROJECT_TARGET_PREREQUISITES)
	$(DOTNET) build -c Debug $(PROJECT_SRC_DIR)/$(PROJECT_NAME).csproj

.PHONY: project-target-mod-clean
project-target-mod-clean:
	$(DOTNET) clean -c Debug $(PROJECT_SRC_DIR)/$(PROJECT_NAME).csproj

## Release
.PHONY: project-target-release-all
project-target-release-all: $(PROJECT_TARGET_PREREQUISITES)
	$(DOTNET) run --project $(PROJECT_CAKE_SRC_DIR)/CakeBuild.csproj

.PHONY: project-target-release-clean
project-target-release-clean:
	$(DOTNET) clean -c Release $(PROJECT_SRC_DIR)/$(PROJECT_NAME).csproj
	rm -drf $(PROJECT_RELEASES_DIR)