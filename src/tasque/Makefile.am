CSC = $(DMCS)

TARGET = Tasque.exe
WRAPPER = tasque

if GTK_2_12
GTK_2_12_CSFLAGS = -define:GTK_2_12
endif

if ENABLE_DEBUG
CSFLAGS = -debug -d:DEBUG
MDBFILES = $(TARGET).mdb
endif

if ENABLE_NOTIFY_SHARP
NOTIFY_SHARP_CSFLAGS = -define:ENABLE_NOTIFY_SHARP
endif

if ENABLE_APPINDICATOR
APPINDICATOR_CSFILES = AppIndicatorTray.cs
APPINDICATOR_CSFLAGS = -define:APPINDICATOR
endif

CSFILES = \
	AbstractTask.cs \
	AllCategory.cs \
	Application.cs \
	CellRendererDate.cs \
	CompletedTaskGroup.cs \
	DateButton.cs \
	GtkApplication.cs \
	GtkTray.cs \
	GnomeApplication.cs \
	Logger.cs \
	NoteDialog.cs \
	NoteWidget.cs \
	Preferences.cs \
	PreferencesDialog.cs \
	RemoteControl.cs \
	RemoteControlProxy.cs \
	StatusIconTray.cs \
	TaskCalendar.cs \
	TaskGroupModel.cs \
	CompletedTaskGroupModel.cs \
	TaskGroupModelFactory.cs \
	TaskGroup.cs \
	TaskWindow.cs \
	TaskTreeView.cs \
	Utilities.cs \
	$(APPINDICATOR_CSFILES)

RESOURCES = \
	-resource:$(top_srcdir)/data/images/tasque-16.png \
	-resource:$(top_srcdir)/data/images/tasque-22.png \
	-resource:$(top_srcdir)/data/images/tasque-24.png \
	-resource:$(top_srcdir)/data/images/tasque-32.png \
	-resource:$(top_srcdir)/data/images/tasque-48.png \
	-resource:$(top_srcdir)/data/images/note-16.png,note.png \
	-resource:$(top_srcdir)/data/images/rtmLogo.png \
	-resource:$(top_srcdir)/data/images/clock-16-0.png \
	-resource:$(top_srcdir)/data/images/clock-16-1.png \
	-resource:$(top_srcdir)/data/images/clock-16-2.png \
	-resource:$(top_srcdir)/data/images/clock-16-3.png \
	-resource:$(top_srcdir)/data/images/clock-16-4.png \
	-resource:$(top_srcdir)/data/images/clock-16-5.png \
	-resource:$(top_srcdir)/data/images/clock-16-6.png \
	-resource:$(top_srcdir)/data/images/clock-16-7.png \
	-resource:$(top_srcdir)/data/images/clock-16-8.png \
	-resource:$(top_srcdir)/data/images/clock-16-9.png \
	-resource:$(top_srcdir)/data/images/clock-16-10.png \
	-resource:$(top_srcdir)/data/images/clock-16-11.png

ASSEMBLIES = \
	-r:System \
	-r:System.Core \
	-r:Mono.Posix \
	-r:System.Xml \
	-r:../libtasque/libtasque.dll \
	$(GLIB_SHARP_20_LIBS) \
	$(GNOME_SHARP_20_LIBS) \
	$(GTK_DOTNET_20_LIBS) \
	$(NOTIFY_SHARP_LIBS) \
	$(NDESK_DBUS_10_LIBS) \
	$(NDESK_DBUS_GLIB_10_LIBS) \
	$(APPINDICATOR_SHARP_LIBS)

$(TARGET): $(CSFILES) Defines.cs
	$(CSC) -unsafe -out:$@ -noconfig -codepage:utf8 -warn:4 $(CSFLAGS) $(GTK_2_12_CSFLAGS) \
	$(NOTIFY_SHARP_CSFLAGS) $(APPINDICATOR_CSFLAGS) $^ $(ASSEMBLIES) $(RESOURCES)

tasquelibdir = $(libdir)/tasque
tasquelib_DATA = $(TARGET) $(TARGET).config $(MDBFILES)

bin_SCRIPTS = $(WRAPPER)

$(WRAPPER): $(WRAPPER).in Makefile
	sed -e "s|\@prefix\@|$(prefix)|g"               \
		-e "s|\@exec_prefix\@|$(bindir)|g"			\
		-e "s|\@libdir\@|$(libdir)|g"				\
		-e "s|\@pkglibdir\@|$(pkglibdir)|g"         \
		-e "s|\@bindir\@|$(bindir)|g"               \
		-e "s|\@target\@|$(TARGET)|g"               \
		-e "s|\@wrapper\@|$(WRAPPER)|g"             \
		-e "s|\@srcdir\@|$(PWD)|g"  \
		< $< > $@
	chmod +x $(WRAPPER)

$(TARGET).mdb: $(TARGET)

$(TARGET).config: $(TARGET).config.in Makefile
	sed -e "s|\@pkglibdir\@|$(pkglibdir)|" \
	    < $< > $@

Defines.cs: Defines.cs.in Makefile
	sed -e "s|\@version\@|$(VERSION)|"     \
		-e "s|\@datadir\@|$(datadir)|"     \
		-e "s|\@pkglibdir\@|$(pkglibdir)|" \
		< $< > $@

EXTRA_DIST = \
	$(CSFILES) \
	$(WRAPPER).in \
	$(TARGET).config.in \
	Defines.cs.in \
	tasque.pc.in \
	OSXApplication.cs

pkgconfigdir = $(libdir)/pkgconfig
pkgconfig_DATA = tasque.pc

CLEANFILES = \
	$(TARGET) \
	$(TARGET).config \
	$(TARGET).mdb \
	$(WRAPPER) \
	Defines.cs

DISTCLEANFILES = \
    $(WRAPPER) \
	$(TARGET) \
	$(TARGET).config \
	$(TARGET).mdb \
	Defines.cs
