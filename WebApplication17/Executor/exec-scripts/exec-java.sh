#!/bin/sh

filename=$1

#ls -la
#cat "$filename.java"
mkdir "build"
javac -cp "./gson-2.13.1.jar" -d build "$filename.java"
cd build || exit 
java -cp "../gson-2.13.1.jar:." "$filename"