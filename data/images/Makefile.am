
pixmapdir = $(datadir)/pixmaps
pixmap_DATA = 	tasque-16.png \
		tasque-32.png

hicolordir = $(datadir)/icons/hicolor
hicolordir_local = $(datadir)/tasque/icons/hicolor

tango_icons =	tasque-16.png \
		tasque-22.png \
		tasque-24.png \
		tasque-32.png \
		tasque-48.png \
		tasque.svg \
		tasque-note.png \
		tasque-rtm-logo.png \
		tasque-panel-16.png \
		tasque-panel-22.png \
		tasque-panel-24.png

clock_icons = tasque-completing-0.png \
		tasque-completing-1.png \
		tasque-completing-2.png \
		tasque-completing-3.png \
		tasque-completing-4.png \
		tasque-completing-5.png \
		tasque-completing-6.png \
		tasque-completing-7.png \
		tasque-completing-8.png \
		tasque-completing-9.png \
		tasque-completing-10.png \
		tasque-completing-11.png 

install-data-local:
	@-$(mkinstalldirs) $(DESTDIR)$(hicolordir)/16x16/apps $(DESTDIR)$(hicolordir)/22x22/apps $(DESTDIR)$(hicolordir)/24x24/apps $(DESTDIR)$(hicolordir)/32x32/apps $(DESTDIR)$(hicolordir)/48x48/apps $(DESTDIR)$(hicolordir)/scalable/apps $(DESTDIR)$(hicolordir_local)/16x16/status $(DESTDIR)$(hicolordir_local)/128x128/places
	$(INSTALL_DATA) $(srcdir)/tasque-16.png $(DESTDIR)$(hicolordir)/16x16/apps/tasque.png
	$(INSTALL_DATA) $(srcdir)/tasque-22.png $(DESTDIR)$(hicolordir)/22x22/apps/tasque.png
	$(INSTALL_DATA) $(srcdir)/tasque-24.png $(DESTDIR)$(hicolordir)/24x24/apps/tasque.png
	$(INSTALL_DATA) $(srcdir)/tasque-32.png $(DESTDIR)$(hicolordir)/32x32/apps/tasque.png
	$(INSTALL_DATA) $(srcdir)/tasque-48.png $(DESTDIR)$(hicolordir)/48x48/apps/tasque.png
	$(INSTALL_DATA) $(srcdir)/tasque.svg $(DESTDIR)$(hicolordir)/scalable/apps/tasque.svg
	$(INSTALL_DATA) $(srcdir)/tasque-panel-16.png $(DESTDIR)$(hicolordir)/16x16/apps/tasque-panel.png
	$(INSTALL_DATA) $(srcdir)/tasque-panel-22.png $(DESTDIR)$(hicolordir)/22x22/apps/tasque-panel.png
	$(INSTALL_DATA) $(srcdir)/tasque-panel-24.png $(DESTDIR)$(hicolordir)/24x24/apps/tasque-panel.png
	$(INSTALL_DATA) $(srcdir)/tasque-note.png $(DESTDIR)$(hicolordir_local)/16x16/status/tasque-note.png
	$(INSTALL_DATA) $(srcdir)/tasque-rtm-logo.png $(DESTDIR)$(hicolordir_local)/128x128/places/tasque-rtm-logo.png
	$(INSTALL_DATA) $(srcdir)/tasque-completing-0.png $(DESTDIR)$(hicolordir_local)/16x16/status/tasque-completing-0.png
	$(INSTALL_DATA) $(srcdir)/tasque-completing-1.png $(DESTDIR)$(hicolordir_local)/16x16/status/tasque-completing-1.png
	$(INSTALL_DATA) $(srcdir)/tasque-completing-2.png $(DESTDIR)$(hicolordir_local)/16x16/status/tasque-completing-2.png
	$(INSTALL_DATA) $(srcdir)/tasque-completing-3.png $(DESTDIR)$(hicolordir_local)/16x16/status/tasque-completing-3.png
	$(INSTALL_DATA) $(srcdir)/tasque-completing-4.png $(DESTDIR)$(hicolordir_local)/16x16/status/tasque-completing-4.png
	$(INSTALL_DATA) $(srcdir)/tasque-completing-5.png $(DESTDIR)$(hicolordir_local)/16x16/status/tasque-completing-5.png
	$(INSTALL_DATA) $(srcdir)/tasque-completing-6.png $(DESTDIR)$(hicolordir_local)/16x16/status/tasque-completing-6.png
	$(INSTALL_DATA) $(srcdir)/tasque-completing-7.png $(DESTDIR)$(hicolordir_local)/16x16/status/tasque-completing-7.png
	$(INSTALL_DATA) $(srcdir)/tasque-completing-8.png $(DESTDIR)$(hicolordir_local)/16x16/status/tasque-completing-8.png
	$(INSTALL_DATA) $(srcdir)/tasque-completing-9.png $(DESTDIR)$(hicolordir_local)/16x16/status/tasque-completing-9.png
	$(INSTALL_DATA) $(srcdir)/tasque-completing-10.png $(DESTDIR)$(hicolordir_local)/16x16/status/tasque-completing-10.png
	$(INSTALL_DATA) $(srcdir)/tasque-completing-11.png $(DESTDIR)$(hicolordir_local)/16x16/status/tasque-completing-11.png

gtk_update_icon_cache = gtk-update-icon-cache -f -t $(hicolordir); gtk-update-icon-cache -f -t $(hicolordir_local)

install-data-hook: update-icon-cache
uninstall-hook: update-icon-cache
update-icon-cache:
	@-if test -z "$(DESTDIR)"; then \
		echo "Updating Gtk icon cache."; \
		$(gtk_update_icon_cache); \
	else \
		echo "*** Icon cache not updated.  After install, run this:"; \
		echo "***   $(gtk_update_icon_cache)"; \
	fi

uninstall-local:
	rm -f $(DESTDIR)$(hicolordir)/16x16/apps/tasque.png
	rm -f $(DESTDIR)$(hicolordir)/22x22/apps/tasque.png
	rm -f $(DESTDIR)$(hicolordir)/24x24/apps/tasque.png
	rm -f $(DESTDIR)$(hicolordir)/32x32/apps/tasque.png
	rm -f $(DESTDIR)$(hicolordir)/48x48/apps/tasque.png
	rm -f $(DESTDIR)$(hicolordir)/scalable/apps/tasque.svg
	rm -f $(DESTDIR)$(hicolordir)/16x16/apps/tasque-panel.png
	rm -f $(DESTDIR)$(hicolordir)/22x22/apps/tasque-panel.png
	rm -f $(DESTDIR)$(hicolordir)/24x24/apps/tasque-panel.png
	rm -f $(DESTDIR)$(hicolordir_local)/16x16/status/tasque-note.png
	rm -f $(DESTDIR)$(hicolordir_local)/128x128/places/tasque-rtm-logo.png
	rm -f $(DESTDIR)$(hicolordir_local)/16x16/status/tasque-completing-0.png
	rm -f $(DESTDIR)$(hicolordir_local)/16x16/status/tasque-completing-1.png
	rm -f $(DESTDIR)$(hicolordir_local)/16x16/status/tasque-completing-2.png
	rm -f $(DESTDIR)$(hicolordir_local)/16x16/status/tasque-completing-3.png
	rm -f $(DESTDIR)$(hicolordir_local)/16x16/status/tasque-completing-4.png
	rm -f $(DESTDIR)$(hicolordir_local)/16x16/status/tasque-completing-5.png
	rm -f $(DESTDIR)$(hicolordir_local)/16x16/status/tasque-completing-6.png
	rm -f $(DESTDIR)$(hicolordir_local)/16x16/status/tasque-completing-7.png
	rm -f $(DESTDIR)$(hicolordir_local)/16x16/status/tasque-completing-8.png
	rm -f $(DESTDIR)$(hicolordir_local)/16x16/status/tasque-completing-9.png
	rm -f $(DESTDIR)$(hicolordir_local)/16x16/status/tasque-completing-10.png
	rm -f $(DESTDIR)$(hicolordir_local)/16x16/status/tasque-completing-11.png

noinst_DATA =

EXTRA_DIST = $(noinst_DATA) $(pixmap_DATA) $(tango_icons) $(clock_icons)
