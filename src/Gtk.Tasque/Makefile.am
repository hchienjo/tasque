CSC = $(DMCS)

TARGET = Gtk.Tasque.dll

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
	Properties/AssemblyInfo.cs \
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
	-resource:$(top_srcdir)/data/images/tasque-note.png \
	-resource:$(top_srcdir)/data/images/tasque-rtm-logo.png \
	-resource:$(top_srcdir)/data/images/tasque-completing-0.png \
	-resource:$(top_srcdir)/data/images/tasque-completing-1.png \
	-resource:$(top_srcdir)/data/images/tasque-completing-2.png \
	-resource:$(top_srcdir)/data/images/tasque-completing-3.png \
	-resource:$(top_srcdir)/data/images/tasque-completing-4.png \
	-resource:$(top_srcdir)/data/images/tasque-completing-5.png \
	-resource:$(top_srcdir)/data/images/tasque-completing-6.png \
	-resource:$(top_srcdir)/data/images/tasque-completing-7.png \
	-resource:$(top_srcdir)/data/images/tasque-completing-8.png \
	-resource:$(top_srcdir)/data/images/tasque-completing-9.png \
	-resource:$(top_srcdir)/data/images/tasque-completing-10.png \
	-resource:$(top_srcdir)/data/images/tasque-completing-11.png

ASSEMBLIES = \
	-r:System \
	-r:System.Core \
	-r:Mono.Posix \
	-r:System.Xml \
	-r:../libtasque/libtasque.dll \
	$(GLIB_SHARP_20_LIBS) \
	$(GNOME_SHARP_20_LIBS) \
	$(GTK_SHARP_20_LIBS) \
	$(NOTIFY_SHARP_LIBS) \
	$(NDESK_DBUS_10_LIBS) \
	$(NDESK_DBUS_GLIB_10_LIBS) \
	$(APPINDICATOR_SHARP_LIBS)

$(TARGET): $(CSFILES) Defines.cs
	$(CSC) -unsafe -out:$@ -t:library -noconfig -codepage:utf8 -warn:4 $(CSFLAGS) $(GTK_2_12_CSFLAGS) \
	$(NOTIFY_SHARP_CSFLAGS) $(APPINDICATOR_CSFLAGS) $^ $(ASSEMBLIES) $(RESOURCES)

tasquelibdir = $(libdir)/tasque
tasquelib_DATA = $(TARGET) $(MDBFILES)

$(TARGET).mdb: $(TARGET)

Defines.cs: Defines.cs.in Makefile
	sed -e "s|\@version\@|$(VERSION)|"     \
		-e "s|\@datadir\@|$(datadir)|"     \
		-e "s|\@pkglibdir\@|$(pkglibdir)|" \
		< $< > $@

EXTRA_DIST = \
	$(CSFILES) \
	Defines.cs.in \
	OSXApplication.cs

CLEANFILES = \
	$(TARGET) \
	$(TARGET).mdb \
	Defines.cs

DISTCLEANFILES = \
	$(TARGET) \
	$(TARGET).mdb \
	Defines.cs
