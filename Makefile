TARGET=IWannaUseTheBrakeWell.exe
OPTIONS= \
	/target:winexe \
	/optimize+ \
	/warn:4 \
	/codepage:65001 \
	/reference:TrainCrewInput.dll

SOURCES= \
	IWannaUseTheBrakeWell.cs

$(TARGET): $(SOURCES)
	csc /out:$@ $(OPTIONS) $^
