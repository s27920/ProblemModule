#!/bin/sh
# arg1 = language used; Ensures correct execution script is used;
# arg2 = unique deployment id;
# TODO Could use an even more isolated environment when deploying to prod, like for instance a read-only filesystem and disallowed any network access
docker run -i --rm --name "$1-$2" --memory 256m --cpus 0.5 "$1-executor" 