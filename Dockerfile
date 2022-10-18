FROM ubuntu:18.04

ARG DEBIAN_FRONTEND=noninteractive

COPY Builds/EdgegapServer /root/build/

WORKDIR /root/

RUN chmod +x /root/build/ServerBuild

ENTRYPOINT [ "/root/build/ServerBuild", "-batchmode", "-nographics"]
