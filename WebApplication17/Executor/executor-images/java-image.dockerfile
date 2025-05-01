FROM eclipse-temurin:17-jdk-jammy

WORKDIR /app

ENV SRC_FILENAME=code
ENV FILE_EXTENSION=java

RUN apt update && \
    apt upgrade -y && \
    apt install -y --no-install-recommends curl && \
    curl -o "./gson-2.13.1.jar" https://repo1.maven.org/maven2/com/google/code/gson/gson/2.13.1/gson-2.13.1.jar && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/* 

COPY ./exec-scripts/exec-java.sh ./ 
COPY ./scripts/stdin-receiver.sh ./

RUN chmod +x *.sh && \
    chmod a+r gson-2.13.1.jar && \
    chmod a-w *.sh gson-2.13.1.jar 

USER nobody:nogroup

ENTRYPOINT ["/bin/sh", "-c", "./stdin-receiver.sh ${FILE_EXTENSION} ${SRC_FILENAME} && ./exec-java.sh  ${SRC_FILENAME}"] 