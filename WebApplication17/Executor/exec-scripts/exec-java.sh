#!/bin/sh

filename=$1

ls -la
mkdir "build"
javac -cp "./gson-2.13.1.jar" -d build "$filename.java"
ls -la
pwd
find / -name "Main.class" 2>/dev/null
cd build || exit 
ls -la
java -cp ".:../gson-2.13.1.jar" "$filename"

