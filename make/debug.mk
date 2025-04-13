.PHONY: debug
debug:
	$(foreach v, $(sort $(.VARIABLES)), \
		$(if $(and \
			$(filter-out undefined,$(origin $(v))), \
			$(filter-out default automatic,$(origin $(v)))), \
			$(info $(v) = $($(v)))))