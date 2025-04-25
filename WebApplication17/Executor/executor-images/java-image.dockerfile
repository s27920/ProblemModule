FROM openjdk:17-jdk-alpine

WORKDIR /app

# TODO why is this a "_" instead of "-"
COPY ./exec-scripts/exec-java.sh execute_java.sh 
COPY ./scripts/stdin-receiver.sh stdin-receiver.sh
RUN chmod +x execute_java.sh && \
    chmod +x stdin-receiver.sh && \
    apk update \
    apk upgrade \
    apk add curl \
    curl https://repo1.maven.org/maven2/com/google/code/gson/gson/2.13.1/gson-2.13.1.jar && \
    echo "installed gson"
#gson installed for serializing function returns to gson giving us nice, deterministic, consistent formats to work with

ENV SRC_FILENAME=code
ENV FILE_EXTENSION=java

#TODO probs make this a custom entrypoint
ENTRYPOINT ["/bin/sh", "-c", "./stdin-receiver.sh ${FILE_EXTENSION} ${SRC_FILENAME} && ./execute_java.sh ${SRC_FILENAME}"] 
#code in here is the src filename 
