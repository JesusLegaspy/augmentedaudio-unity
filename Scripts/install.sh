#! /bin/sh
# https://netstorage.unity3d.com/unity/5d30cf096e79/MacEditorInstaller/Unity-2017.1.1f1.pkg

BASE_URL=http://netstorage.unity3d.com/unity
HASH=5d30cf096e79
VERSION=2017.1.1f1

download() {
  file=$1
  url="$BASE_URL/$HASH/$package"

  echo "Downloading from $url: "
  curl -o `basename "$package"` "$url"
}

install() {
  package=$1
  download "$package"

  echo "Installing "`basename "$package"`
  sudo installer -dumplog -package `basename "$package"` -target /
}

# See $BASE_URL/$HASH/unity-$VERSION-$PLATFORM.ini for complete list
# of available packages, where PLATFORM is `osx` or `win`

echo "Installing Unity..."
install "MacEditorInstaller/Unity-$VERSION.pkg"
echo "Installing Windows support packages..."
install "MacEditorTargetInstaller/UnitySetup-Windows-Support-for-Editor-$VERSION.pkg"
#install "MacEditorTargetInstaller/UnitySetup-Mac-Support-for-Editor-$VERSION.pkg"
#install "MacEditorTargetInstaller/UnitySetup-Linux-Support-for-Editor-$VERSION.pkg"
