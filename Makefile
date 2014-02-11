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
PDFLATEX   := /usr/local/texlive/2012/bin/x86_64-darwin/pdflatex

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

doc: build
	mkdir -p package/Kalculator/doc
	cp doc/*.mkd doc/*.png package/Kalculator/doc
	cd doc; ${PDFLATEX} space; ${PDFLATEX} space
	cp doc/space.pdf package/Kalculator/doc/math_notes.pdf

package: build doc
	mkdir -p package/Kalculator/Plugins
	mkdir -p package/Kalculator/Textures
	cp build/Kalculator.dll package/Kalculator/Plugins/
	cp icons/*.png package/Kalculator/Textures/
	cp README.md package/Kalculator
	cp LICENSE.md package/Kalculator


tar.gz: package
	cd package; ${TAR} zcf Kalculator-$(shell ${GIT} describe --tags).tar.gz Kalculator

zip: package
	cd package; ${ZIP} -9 -r Kalculator-$(shell ${GIT} describe --tags).zip Kalculator

clean:
	@echo "Cleaning up build and package directories..."
	rm -rf build/ package/

install: package
	mkdir -p ${KSPDIR}/GameData/
	cp -rv package/Kalculator ${KSPDIR}/GameData/

uninstall: info
	rm -rf ${KSPDIR}/GameData/Kalculator

test: info
	${GMCS} Kalculator.cs Function.cs Variable.cs Tokenizer.cs Globals.cs
	${MONO} Kalculator.exe tests

unity: 
	cp Kalculator.cs Function.cs Variable.cs Tokenizer.cs Globals.cs ~/Calculator/Assets/Standard\ Assets/
	cp KalculatorGUI.cs ~/Calculator/Assets/

release: zip tar.gz
	cp Kalculator-$(shell ${GIT} describe --tags).zip ~/Dropbox/Public/Kalculator
	cp Kalculator-$(shell ${GIT} describe --tags).tar.gz ~/Dropbox/Public/Kalculator

.PHONY : all info doc build package tar.gz zip clean install uninstall
