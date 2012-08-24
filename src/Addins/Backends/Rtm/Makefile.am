CSC = $(DMCS)

CSFILES = *.cs

ASSEMBLIES = \
	-r:../../../Tasque.exe \
	-r:../../../../RtmNet/RtmNet.dll \
	-r:System \
	-r:Mono.Posix \
	$(GTK_DOTNET_20_LIBS)

if ENABLE_BACKEND_RTM

TARGET = RtmBackend.dll

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
