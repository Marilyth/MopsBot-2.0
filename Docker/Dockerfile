FROM ubuntu:20.04
ENV DEBIAN_FRONTEND=noninteractive

RUN  apt-get update \
  && apt-get install -y wget \
  && rm -rf /var/lib/apt/lists/*

RUN apt update
RUN apt-get update
RUN apt-get install -y imagemagick
RUN sed -i '/disable ghostscript format types/,+6d' /etc/ImageMagick-6/policy.xml
COPY ./Docker/execute.sh ./
RUN chmod +x ./execute.sh
COPY ./bin/Debug/netcoreapp3.1/linux-x64/publish /publish
EXPOSE 5000
CMD ["./execute.sh"]