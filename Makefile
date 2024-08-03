TARGET=IWannaUseTheBrakeWell.exe
OPTIONS= \
	/target:winexe \
	/optimize+ \
	/warn:4 \
	/codepage:65001 \
	/win32icon:bu.ico \
	/reference:TrainCrewInput.dll

SOURCES= \
	AssemblyInfo.cs \
	IWannaUseTheBrakeWell.cs

$(TARGET): $(SOURCES)
	csc /out:$@ $(OPTIONS) $^
