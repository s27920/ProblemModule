FROM eclipse-temurin:17-jdk-jammy

WORKDIR /app

ENV SRC_FILENAME=code
ENV FILE_EXTENSION=java

COPY ./exec-scripts/exec-java.sh execute-java.sh 
COPY ./scripts/stdin-receiver.sh stdin-receiver.sh

RUN apt update && \
    apt upgrade -y && \
    apt install -y --no-install-recommends curl && \
    curl -o "./gson-2.13.1.jar" https://repo1.maven.org/maven2/com/google/code/gson/gson/2.13.1/gson-2.13.1.jar
   
ENTRYPOINT ["/bin/sh", "-c", "./stdin-receiver.sh ${FILE_EXTENSION} ${SRC_FILENAME} && ./execute-java.sh  ${SRC_FILENAME}"] 