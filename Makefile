# Makefile for building Kalculator

KSPDIR  := ${HOME}/Library/Application\ Support/Steam/SteamApps/common/Kerbal\ Space\ Program
MANAGED := KSP.app/Contents/Data/Managed/

SOURCEFILES := $(wildcard *.cs)

RESGEN2 := /usr/bin/resgen2
GMCS    := /usr/bin/gmcs
MONO    := /usr/bin/mono
GIT     := /usr/bin/git
TAR     := /usr/bin/tar
ZIP     := /usr/bin/zip

all: build

info:
	@echo "== Kalculator Build Information =="
	@echo "  resgen2: ${RESGEN2}"
	@echo "  gmcs:    ${GMCS}"
	@echo "  git:     ${GIT}"
	@echo "  tar:     ${TAR}"
	@echo "  zip:     ${ZIP}"
	@echo "  KSP Data: ${KSPDIR}"
	@echo "  Source: ${SOURCEFILES}"
	@echo "==================================="

build: info
	mkdir -p build
	${GMCS} -t:library -lib:${KSPDIR}/${MANAGED} \
		-r:Assembly-CSharp,Assembly-CSharp-firstpass,UnityEngine \
		-out:build/Kalculator.dll \
		${SOURCEFILES}

package: build
	mkdir -p package/Kalculator/Plugins
	cp build/Kalculator.dll package/Kalculator/Plugins/

tar.gz: package
	${TAR} zcf Kalculator-$(shell ${GIT} describe --tags --long --always).tar.gz package/Kalculator

zip: package
	${ZIP} -9 -r Kalculator-$(shell ${GIT} describe --tags --long --always).zip package/Kalculator

clean:
	@echo "Cleaning up build and package directories..."
	rm -rf build/ package/

install: build
	mkdir -p ${KSPDIR}/GameData/Kalculator/Plugins
	cp build/Kalculator.dll ${KSPDIR}/GameData/Kalculator/Plugins/

uninstall: info
	rm -rf ${KSPDIR}/GameData/Kalculator/Plugins

test: info
	${GMCS} Kalculator.cs Function.cs Variable.cs Tokenizer.cs Globals.cs
	${MONO} Kalculator.exe tests

unity: 
	cp Kalculator.cs Function.cs Variable.cs Tokenizer.cs Globals.cs ~/Calculator/Assets/Standard\ Assets/
	cp KalculatorGUI.cs ~/Calculator/Assets/


.PHONY : all info build package tar.gz zip clean install uninstall
