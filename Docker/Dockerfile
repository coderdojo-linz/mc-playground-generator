ARG JDK_IMAGE=openjdk
ARG JDK_TAG=17-bullseye
FROM ${JDK_IMAGE}:${JDK_TAG}
ARG CODE_SERVER_VER=3.12.0

WORKDIR /app

RUN curl -fOL https://github.com/cdr/code-server/releases/download/v${CODE_SERVER_VER}/code-server_${CODE_SERVER_VER}_amd64.deb \
    && dpkg -i code-server_${CODE_SERVER_VER}_amd64.deb \
    && rm code-server_${CODE_SERVER_VER}_amd64.deb

RUN curl -O http://dev.slothyx.com/server.zip \
    && unzip server.zip \
    && rm server.zip

RUN apt update -y && apt install -y jq && apt clean 

COPY ./config.yaml /root/.config/code-server/config.yaml

COPY ./start_script.sh ./

EXPOSE 8080
EXPOSE 25565

CMD ["./start_script.sh"]
