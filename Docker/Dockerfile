FROM ubuntu:22.04
ENV DEBIAN_FRONTEND=noninteractive

# Install dependencies
RUN apt-get update \
 && apt-get install -y wget curl nodejs \
 && wget https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb \
 && dpkg -i packages-microsoft-prod.deb \
 && rm packages-microsoft-prod.deb \
 && apt-get update \
 && apt-get install -y apt-transport-https \
 && apt-get install -y imagemagick \
 && apt-get install -y dotnet-sdk-5.0 \
 && wget -qO- https://www.npmjs.com/install.sh | sh \
 && npm install -g n && n stable \
 && npm install -g localtunnel \
 && rm -rf /var/lib/apt/lists/* \
 && sed -i '/disable ghostscript format types/,+6d' /etc/ImageMagick-6/policy.xml

# Copy Mops code to image
COPY ./ /MopsCode

# Compile Mops
RUN cd /MopsCode \
 && dotnet publish -r linux-x64 -p:PublishSingleFile=true --self-contained true -o /publish \
 && rm -r /MopsCode

COPY ./Docker/execute.sh ./execute.sh
RUN chmod +x ./execute.sh

EXPOSE 5000
CMD ["./execute.sh"]
