#! /bin/sh

project="Poly"

echo "Attempting to build $project for OS X"
/Applications/Unity/Unity.app/Contents/MacOS/Unity
  -batchmode
  -nographics
  -silent-crashes
  -logFile $(pwd)/unity.log
  -projectPath $(pwd)
  -buildOSXUniversalPlayer "$(pwd)/Build/osx/$project.app"
  -quit


echo 'Logs from build'
cat $(pwd)/unity.log


# echo 'Attempting to zip builds'
# zip -r $(pwd)/Build/mac.zip $(pwd)/Build/osx/
