.PHONY: dotnet-restore
dotnet-restore:
	$(foreach proj_name,$(PROJECT_NAME_LIST),\
		$(DOTNET) restore $(SRC_DIR)/$(proj_name)/$(proj_name).sln $(newline)\
	)