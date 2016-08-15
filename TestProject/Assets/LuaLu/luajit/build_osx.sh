#!/bin/sh
DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
LIPO="xcrun -sdk macosx lipo"
STRIP="xcrun -sdk macosx strip"

SRCDIR=$DIR/src
DESTDIR=$DIR/prebuilt/osx

rm "$DESTDIR"/*.a
cd $SRCDIR

# build x86 lib
make clean
make CC="gcc -m32 -arch i386" clean all
mv "$SRCDIR"/src/libluajit.a "$DESTDIR"/libluajit-i386.a

# build x64 lib
make clean
make CC="gcc -arch x86_64" clean all
mv "$SRCDIR"/src/libluajit.a "$DESTDIR"/libluajit-x86_64.a

# fat lib
$LIPO -create "$DESTDIR"/libluajit-*.a -output "$DESTDIR"/libluajit.a
$STRIP -S "$DESTDIR"/libluajit.a
$LIPO -info "$DESTDIR"/libluajit.a

# clean
make clean

# build bundle
cd "$DESTDIR"
rm -rf lualu.bundle
cd lualu
rm -rf build
xcodebuild

# copy bundle out and clean
cp -r build/Release/lualu.bundle ..
rm -rf build
rm "$DESTDIR"/libluajit*.a
