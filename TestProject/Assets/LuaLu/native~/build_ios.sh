#!/bin/sh
DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
LIPO="xcrun -sdk iphoneos lipo"
STRIP="xcrun -sdk iphoneos strip"

SRCDIR=$DIR/luajit
DESTDIR=$DIR/ios_lib_proj
SDK_PARAM=`xcodebuild -showsdks | awk '/^$/{p=0};p; /iOS SDKs:/{p=1}' | tail -1 | cut -f3`
SYSROOT=`xcodebuild -version $SDK_PARAM Path`

rm "$DESTDIR"/*.a
cd $SRCDIR

make clean
ISDKF="-arch armv7 -isysroot $SYSROOT -miphoneos-version-min=7.0"
make HOST_CC="gcc -m32" TARGET_FLAGS="$ISDKF" TARGET_SYS=iOS
mv "$SRCDIR"/src/libluajit.a "$DESTDIR"/libluajit-armv7.a

make clean
ISDKF="-arch armv7s -isysroot $SYSROOT -miphoneos-version-min=7.0"
make HOST_CC="gcc -m32" TARGET_FLAGS="$ISDKF" TARGET_SYS=iOS
mv "$SRCDIR"/src/libluajit.a "$DESTDIR"/libluajit-armv7s.a

make clean
ISDKF="-arch arm64 -isysroot $SYSROOT -miphoneos-version-min=7.0"
make HOST_CC="gcc" TARGET_FLAGS="$ISDKF" TARGET_SYS=iOS
mv "$SRCDIR"/src/libluajit.a "$DESTDIR"/libluajit-arm64.a

make clean
make CC="gcc -m32 -arch i386" clean all
mv "$SRCDIR"/src/libluajit.a "$DESTDIR"/libluajit-i386.a

make clean
make CC="gcc -arch x86_64" clean all
mv "$SRCDIR"/src/libluajit.a "$DESTDIR"/libluajit-x86_64.a

$LIPO -create "$DESTDIR"/libluajit-*.a -output "$DESTDIR"/libluajit.a
$STRIP -S "$DESTDIR"/libluajit.a
$LIPO -info "$DESTDIR"/libluajit.a

make clean

# build bundle
cd "$DESTDIR"
rm -rf build
xcodebuild

# copy bundle out and clean
rm -rf ../../Plugins/iOS
mkdir ../../Plugins/iOS
cp -r build/Release-iphoneos/liblualu.a ../../Plugins/iOS
rm -rf build
rm "$DESTDIR"/libluajit*.a
