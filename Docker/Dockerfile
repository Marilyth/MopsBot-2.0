FROM ubuntu:20.04
ENV DEBIAN_FRONTEND=noninteractive

RUN  apt-get update \
  && apt-get install -y wget \
  && rm -rf /var/lib/apt/lists/*

RUN wget https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
RUN dpkg -i packages-microsoft-prod.deb
RUN apt update
RUN apt-get install -y imagemagick
COPY ./Docker/execute.sh ./
RUN chmod +x ./execute.sh
COPY ./bin/Debug/netcoreapp3.1/linux-x64/publish /publish
EXPOSE 5000
CMD ["./execute.sh"]