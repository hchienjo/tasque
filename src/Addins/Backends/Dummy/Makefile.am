CSC = $(DMCS)

CSFILES = \
	AssemblyInfo.cs \
	DummyNote.cs \
	DummyBackend.cs \
	DummyCategory.cs \
	DummyTask.cs

ASSEMBLIES = \
	-r:../../../libtasque/libtasque.dll \
	-r:../../../Gtk.Tasque/Gtk.Tasque.dll \
	-r:System \
	-r:Mono.Posix \
	$(GTK_SHARP_20_LIBS)

if ENABLE_BACKEND_DUMMY

TARGET = DummyBackend.dll

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
