#!/bin/sh

for var in "$@"
do
  docker build -q -t "$var"-executor -f executor-images/"$var"-image.dockerfile .
done