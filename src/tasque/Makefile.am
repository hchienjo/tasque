CSC = $(DMCS)

TARGET = Tasque.exe
WRAPPER = tasque

if ENABLE_DEBUG
CSFLAGS = -debug -d:DEBUG
MDBFILES = $(TARGET).mdb
endif

CSFILES = \
	Properties/AssemblyInfo.cs \
	Program.cs

ASSEMBLIES = \
	-r:System \
	-r:../libtasque/libtasque.dll \
	-r:../Gtk.Tasque/Gtk.Tasque.dll

$(TARGET): $(CSFILES)
	$(CSC) -out:$@ -noconfig -codepage:utf8 -warn:4 $(CSFLAGS) $^ $(ASSEMBLIES)

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

EXTRA_DIST = \
	$(CSFILES) \
	$(WRAPPER).in \
	$(TARGET).config.in \
	tasque.pc.in

pkgconfigdir = $(libdir)/pkgconfig
pkgconfig_DATA = tasque.pc

CLEANFILES = \
	$(TARGET) \
	$(TARGET).config \
	$(TARGET).mdb \
	$(WRAPPER)

DISTCLEANFILES = \
    $(WRAPPER) \
	$(TARGET) \
	$(TARGET).config \
	$(TARGET).mdb
