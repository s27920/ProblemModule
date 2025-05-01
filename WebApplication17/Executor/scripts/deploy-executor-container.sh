#!/bin/sh
# arg1 = language used; Ensures correct execution script is used;
# arg2 = unique deployment id;
# arg3 = name of the public class that contains main
docker run \
  -i \
  -e SRC_FILENAME="$3" \
  -e FILE_EXTENSION="java" \
  --rm \
  --read-only \
  --tmpfs /tmp:rw,noexec,nosuid,size=10M \
  --name "$1-$2" \
  --memory 384m \
  --memory-swap 384m \
  --cpus 0.5 \
  --network none \
  "$1-executor"
