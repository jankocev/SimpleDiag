#!/usr/bin/env bash
set -e

APP_NAME="ServerDiagnostics"
VERSION=${VERSION:-0.0.0}
ARCH="amd64"
RUNTIME="linux-x64"

rm -rf publish package *.deb

# Publish
dotnet publish -c Release -r "$RUNTIME" \
  --self-contained true -o publish

#Create template config
mkdir -p package/etc/$APP_NAME

cat >package/etc/$APP_NAME/config.json <<EOF
{
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://[::]:80"
      }
    }
  },
  "ApiKey": ""
}
EOF

# Package layout
mkdir -p package/DEBIAN
#Mark config template as config
cat >package/DEBIAN/conffiles <<EOF
/etc/$APP_NAME/config.json
EOF
mkdir -p package/usr/local/$APP_NAME
mkdir -p package/usr/bin

# Copy app
cp -r publish/* package/usr/local/$APP_NAME/

# Launcher
cat >package/usr/bin/$APP_NAME <<EOF
#!/bin/sh
exec /usr/bin/dotnet /usr/local/$APP_NAME/${APP_NAME}.dll "\$@"
EOF

chmod +x package/usr/bin/$APP_NAME

# Control file
cat >package/DEBIAN/control <<EOF
Package: $APP_NAME
Version: $VERSION
Section: utils
Priority: optional
Architecture: $ARCH
Maintainer: Jan Kocev <jankocev@seznam.cz>
Depends: dotnet-runtime-8.0
Description: Server diagnostics service
EOF

# Build package
dpkg-deb --build package

mv package.deb ${APP_NAME}_${VERSION}_${ARCH}.deb

echo "Built ${APP_NAME}_${VERSION}_${ARCH}.deb"
