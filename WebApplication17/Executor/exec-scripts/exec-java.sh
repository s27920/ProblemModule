#!/bin/bash

filename=$1

ulimit -u 50


cd /tmp || exit 
javac -cp "/app/gson-2.13.1.jar" -d . "$filename.java"
java -cp ".:/app/gson-2.13.1.jar" -XX:+UseContainerSupport -XX:MaxRAMPercentage=75.0 -Xmx64m "$filename"