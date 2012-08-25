CSC = $(DMCS)

CSFILES = *.cs

ASSEMBLIES = \
	-r:../../../libtasque/libtasque.dll \
	-r:../../../tasque/Tasque.exe \
	-r:System \
	-r:System.Xml \
	-r:Mono.Posix \
	$(GLIB_SHARP_20_LIBS) \
	$(GNOME_SHARP_20_LIBS) \
	$(GTK_DOTNET_20_LIBS) \
	$(ICE_DESKTOP_LIBS) \
	$(NDESK_DBUS_10_LIBS) \
	$(NDESK_DBUS_GLIB_10_LIBS)

if ENABLE_BACKEND_ICECORE

TARGET = IceCoreBackend.dll

if ENABLE_DEBUG
CSFLAGS = -debug -d:DEBUG
MDBFILES = $(TARGET).mdb
endif

$(TARGET): $(CSFILES)
	$(CSC) -out:$@ -t:library -noconfig -codepage:utf8 -warn:4 $(CSFLAGS) $^ $(ASSEMBLIES)
	
$(TARGET).mdb: $(TARGET)

endif

tasquelibdir = $(libdir)/tasque
tasquelib_DATA = $(TARGET) $(MDBFILES)

EXTRA_DIST = $(CSFILES)

CLEANFILES = $(TARGET) $(TARGET).mdb

DISTCLEANFILES = $(TARGET) $(TARGET).mdb
