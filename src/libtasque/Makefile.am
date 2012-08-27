CSC = $(DMCS)

TARGET = libtasque.dll

if ENABLE_DEBUG
CSFLAGS = -debug -d:DEBUG
MDBFILES = $(TARGET).mdb
endif

CSFILES = \
	Properties/AssemblyInfo.cs \
	DateFormatterFactory.cs \
	Extensions.cs \
	IBackend.cs \
	ICategory.cs \
	INativeApplication.cs \
	INote.cs \
	ITask.cs \
	RegularExpressionFormatter.cs \
	TaskParser.cs \
	TaskPriority.cs \
	TaskState.cs \
	TranslatableToken.cs \
	DateFormatters/DateSeparatedFormatter.cs \
	DateFormatters/DayFormatter.cs \
	DateFormatters/DateFormatter.cs \
	DateFormatters/IDateFormatter.cs \
	DateFormatters/MonthFormatter.cs \
	DateFormatters/OrdinalFormatter.cs \
	DateFormatters/QuantityFormatter.cs \
	DateFormatters/StringInsensitiveComparer.cs \
	DateFormatters/TodayTomorrowFormatter.cs \
	DateFormatters/WeekdayFormatter.cs \
	DateFormatters/WeekFormatter.cs \
	DateFormatters/YearFormatter.cs

ASSEMBLIES = \
	-r:System \
	-r:System.Core \
	-r:Mono.Posix \
	$(GTK_DOTNET_20_LIBS)

$(TARGET): $(CSFILES)
	$(CSC) -out:$@ -t:library -noconfig -codepage:utf8 -warn:4 $(CSFLAGS) $^ $(ASSEMBLIES)

tasquelibdir = $(libdir)/tasque
tasquelib_DATA = $(TARGET) $(MDBFILES)

$(TARGET).mdb: $(TARGET)

EXTRA_DIST = $(CSFILES) libtasque.csproj

CLEANFILES = $(TARGET) $(TARGET).mdb

DISTCLEANFILES = $(TARGET) $(TARGET).mdb
