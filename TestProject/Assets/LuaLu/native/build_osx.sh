#!/bin/sh
DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
LIPO="xcrun -sdk macosx lipo"
STRIP="xcrun -sdk macosx strip"

USE=lua
SRCDIR=$DIR/$USE
DESTDIR=$DIR/osx_bundle_proj

rm "$DESTDIR"/*.a
cd $SRCDIR

# build x86 lib
make clean
make macosx HOST_CC="gcc -m32 -arch i386"
mv "$SRCDIR"/src/lib"$USE".a "$DESTDIR"/lib"$USE"-i386.a

# build x64 lib
make clean
make macosx HOST_CC="gcc -arch x86_64"
mv "$SRCDIR"/src/lib"$USE".a "$DESTDIR"/lib"$USE"-x86_64.a

# fat lib
$LIPO -create "$DESTDIR"/lib"$USE"-*.a -output "$DESTDIR"/lib"$USE".a
$STRIP -S "$DESTDIR"/lib"$USE".a
$LIPO -info "$DESTDIR"/lib"$USE".a

# clean
make clean

# build bundle
cd "$DESTDIR"
rm -rf build
xcodebuild

# copy bundle out and clean
rm -rf ../../Plugins/lualu.bundle
cp -r build/Release/lualu.bundle ../../Plugins
rm -rf build
rm "$DESTDIR"/lib"$USE"*.a
