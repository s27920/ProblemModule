#! /bin/sh

filename=$1

mkdir "build"
javac -d build "$filename.java"
cd build || exit 
java "$filename"