#!/bin/sh
# arg1 = language used; Ensures correct execution script is used;
# arg2 = unique deployment id;
# arg3 = name of the public class that contains main
# TODO Could use an even more isolated environment when deploying to prod, like for instance a read-only filesystem and disallowed any network access
docker run \
  -i --rm \
  --name "$1-$2" \
  -e SRC_FILENAME="$3" \
  -e FILE_EXTENSION="java" \
  --memory 256m \
  --cpus 0.5 \
  --network none \
  "$1-executor"
