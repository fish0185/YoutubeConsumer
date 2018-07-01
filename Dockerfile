FROM microsoft/dotnet:2.1-runtime

RUN apt-get update \
  && apt-get install -y python3-pip python3-dev \
  && apt-get -y install locales \
  && apt-get -y install ffmpeg \
  && cd /usr/local/bin \
  && ln -s /usr/bin/python3 python \
  && pip3 install --upgrade pip

# Set the locale
RUN sed -i -e 's/# en_US.UTF-8 UTF-8/en_US.UTF-8 UTF-8/' /etc/locale.gen && \
    locale-gen
ENV LANG en_US.UTF-8  
ENV LANGUAGE en_US:en  
ENV LC_ALL en_US.UTF-8

RUN pip3 install you-get \
  && pip3 install awscli 

WORKDIR /consumer
COPY . .

CMD [ "dotnet", "publish/YoutubeDownloader.dll" ]
