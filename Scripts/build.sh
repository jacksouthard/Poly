#! /bin/sh

project="Poly"

echo "Attempting to build $project for OS X"
/Applications/Unity/Unity.app/Contents/MacOS/Unity -projectPath $(pwd) -buildOSXUniversalPlayer "$(pwd)/Build/osx/$project.app" -quit -logFile $(pwd)/unity.log -batchmode -silent-crashes

echo 'Logs from build'
cat $(pwd)/unity.log

echo "Attempting to run $project as server"
/Users/jack/Poly/Build/osx/Poly.app/Contents/MacOS/Poly 

# echo 'Attempting to zip builds'
# zip -r $(pwd)/Build/mac.zip $(pwd)/Build/osx/
