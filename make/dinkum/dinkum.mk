.PHONY: dinkum-save-backup-all
dinkum-save-backup-all:
	$(shell mkdir -p $(DINKUM_BACKUP))
	cp -r "$(DINKUM_SAVES)"/* "$(DINKUM_BACKUP)"