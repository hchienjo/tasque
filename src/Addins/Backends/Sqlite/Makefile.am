CSC = $(DMCS)

CSFILES = \
	AssemblyInfo.cs \
	SqliteTask.cs \
	Database.cs \
	SqliteCategory.cs \
	SqliteBackend.cs \
	SqliteNote.cs

ASSEMBLIES = \
	-r:../../../libtasque/libtasque.dll \
	-r:../../../tasque/Tasque.exe \
	-r:System \
	-r:System.Data \
	-r:Mono.Data.Sqlite \
	-r:Mono.Posix \
	$(GTK_DOTNET_20_LIBS)

if ENABLE_BACKEND_SQLITE

TARGET = SqliteBackend.dll

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
