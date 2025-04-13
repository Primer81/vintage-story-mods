.PHONY: vintage-story-save-backup-all
vintage-story-save-backup-all:
	$(shell mkdir -p $(VINTAGE_STORY_BACKUP))
	cp -r "$(VINTAGE_STORY_SAVES)"/* "$(VINTAGE_STORY_BACKUP)"

.PHONY: vs-save-backup-all
vs-save-backup-all: vintage-story-save-backup-all