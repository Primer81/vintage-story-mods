.PHONY: project-destroy
project-destroy:
	rm -drf $(PROJECT_DIR)
	rm -f $(PROJECT_CREATE_SENTINEL)

.PHONY: project-create
project-create: $(PROJECT_CREATE_SENTINEL)
$(PROJECT_CREATE_SENTINEL): $(PROJECT_CREATE_PREREQUISITES)
	$(shell mkdir -p $(dir $@))
	$(DOTNET_VSMODE) \
		--AddSampleCode \
		--AddAssetFolder \
		--output $(PROJECT_DIR) \
		--name $(PROJECT_NAME)
	rm -f $(PROJECT_DIR)/build.ps1
	rm -f $(PROJECT_DIR)/build.sh
	touch $@

.PHONY: project-build-all
project-build-all: $(PROJECT_BUILD_PREREQUISITES)
	$(DOTNET) build -c $(PROJECT_BUILD_PROFILE) \
		$(PROJECT_CAKE_SRC_DIR)/CakeBuild.csproj
	$(DOTNET) build -c $(PROJECT_BUILD_PROFILE) \
		$(PROJECT_SRC_DIR)/$(PROJECT_NAME).csproj
	$(DOTNET) run --project $(PROJECT_CAKE_SRC_DIR)/CakeBuild.csproj

.PHONY: project-build-clean
project-build-clean: $(PROJECT_BUILD_PREREQUISITES)
	$(DOTNET) clean -c $(PROJECT_BUILD_PROFILE) \
		$(PROJECT_SRC_DIR)/$(PROJECT_NAME).csproj
	$(DOTNET) clean -c $(PROJECT_BUILD_PROFILE) \
		$(PROJECT_CAKE_SRC_DIR)/CakeBuild.csproj
	rm -drf $(PROJECT_RELEASES_DIR)

.PHONY: project-run-client
project-run-client: $(PROJECT_RUN_PREREQUISITES)
	$(VINTAGE_STORY)/Vintagestory \
		--tracelog \
		--addModPath $(PROJECT_DIR)/bin/$(PROJECT_BUILD_PROFILE)/Mods

.PHONY: project-run-server
project-run-server: $(PROJECT_RUN_PREREQUISITES)
	$(VINTAGE_STORY)/VintagestoryServer \
		--tracelog \
		--addModPath $(PROJECT_DIR)/bin/$(PROJECT_BUILD_PROFILE)/Mods
