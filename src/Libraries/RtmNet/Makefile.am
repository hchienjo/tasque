CSC = $(DMCS)

TARGET = RtmNet.dll

if ENABLE_DEBUG
CSFLAGS = -debug -d:DEBUG
MDBFILES = $(TARGET).mdb
endif

CSFILES = \
	ApiKeyRequiredException.cs		\
	AssemblyInfo.cs			\
	Auth.cs				\
	AuthenticationRequiredException.cs	\
	Categories.cs				\
	Contacts.cs				\
	DateGranularity.cs			\
	Enums.cs				\
	Groups.cs				\
	GroupSearchResults.cs			\
	HttpUtility.cs			\
	List.cs				\
	Methods.cs				\
	Note.cs				\
	Response.cs				\
	ResponseXmlException.cs		\
	RtmApiException.cs			\
	Rtm.cs				\
	RtmException.cs			\
	RtmWebException.cs			\
	SignatureRequiredException.cs		\
	Tags.cs				\
	Task.cs				\
	User.cs				\
	Utils.cs


RESOURCES = 

ASSEMBLIES =  \
	-r:System \
	-r:System.Xml

$(TARGET): $(CSFILES)
	$(CSC) -unsafe -out:$@ -t:library -noconfig -codepage:utf8 -warn:4 $(CSFLAGS) $^ $(ASSEMBLIES) $(RESOURCES)

$(TARGET).mdb: $(TARGET)

tasquelibdir = $(libdir)/tasque
tasquelib_DATA = $(TARGET) $(MDBFILES)

bin_SCRIPTS = $(WRAPPER)


EXTRA_DIST = \
	$(CSFILES)

CLEANFILES = \
	$(TARGET)					\
	$(TARGET).mdb

DISTCLEANFILES =                        \
	$(TARGET)			\
	$(TARGET).mdb
