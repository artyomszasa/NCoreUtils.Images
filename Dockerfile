FROM microsoft/dotnet:2.1-sdk AS build-env-ncimages
WORKDIR /app
COPY ./NCoreUtils.Images.sln ./
COPY ./NCoreUtils.Images/*.fsproj ./NCoreUtils.Images/
COPY ./NCoreUtils.Images.Abstractions/*.fsproj ./NCoreUtils.Images.Abstractions/
COPY ./NCoreUtils.Images.Core/*.fsproj ./NCoreUtils.Images.Core/
COPY ./NCoreUtils.Images.DependencyInjection/*.fsproj ./NCoreUtils.Images.DependencyInjection/
COPY ./NCoreUtils.Images.ImageMagick/*.fsproj ./NCoreUtils.Images.ImageMagick/
COPY ./NCoreUtils.Images.Provider.ImageMagick/*.fsproj ./NCoreUtils.Images.Provider.ImageMagick/
COPY ./NCoreUtils.Images.WebService/*.fsproj ./NCoreUtils.Images.WebService/
COPY ./NCoreUtils.Images.Optimization.External/*.fsproj ./NCoreUtils.Images.Optimization.External/
COPY ./NCoreUtils.Images.WebService.Shared/*.fsproj ./NCoreUtils.Images.WebService.Shared/
RUN dotnet restore NCoreUtils.Images.WebService/NCoreUtils.Images.WebService.fsproj -r linux-x64

COPY ./NCoreUtils.Images/*.fs ./NCoreUtils.Images/
COPY ./NCoreUtils.Images.Abstractions/*.fs ./NCoreUtils.Images.Abstractions/
COPY ./NCoreUtils.Images.Core/*.fs ./NCoreUtils.Images.Core/
COPY ./NCoreUtils.Images.DependencyInjection/*.fs ./NCoreUtils.Images.DependencyInjection/
COPY ./NCoreUtils.Images.ImageMagick/*.fs ./NCoreUtils.Images.ImageMagick/
COPY ./NCoreUtils.Images.Provider.ImageMagick/*.fs ./NCoreUtils.Images.Provider.ImageMagick/
COPY ./NCoreUtils.Images.WebService/*.fs ./NCoreUtils.Images.WebService/
COPY ./NCoreUtils.Images.Optimization.External/*.fs ./NCoreUtils.Images.Optimization.External/
COPY ./NCoreUtils.Images.WebService.Shared/*.fs ./NCoreUtils.Images.WebService.Shared/
RUN dotnet publish NCoreUtils.Images.WebService/NCoreUtils.Images.WebService.fsproj --no-restore -c Release -r linux-x64 -o /app/out

# RUNTIME IMAGE

FROM microsoft/dotnet:2.1-runtime-deps
RUN apt-get update && DEBIAN_FRONTEND=noninteractive apt-get install -y apt-utils
# install imagemagick dependencies
RUN apt-get update \
    && DEBIAN_FRONTEND=noninteractive apt-get install -y --no-install-recommends \
      curl \
      libbsd0 \
      libbz2-1.0 \
      libc6 \
      libcairo2 \
      libcdt5 \
      libcgraph6 \
      libdatrie1 \
      libdjvulibre21 \
      libexpat1 \
      libffi6 \
      libfontconfig1 \
      libfreetype6 \
      libgcc1 \
      libgcc-6-dev \
      libglib2.0-0 \
      libgomp1 \
      libgraphite2-3 \
      libgvc6 \
      libharfbuzz0b \
      libice6 \
      libicu57 \
      libilmbase12 \
      libjbig0 \
      liblcms2-2 \
      libltdl7 \
      liblzma5 \
      libopenexr22 \
      libopenjp2-7 \
      libpango-1.0-0 \
      libpangocairo-1.0-0 \
      libpangoft2-1.0-0 \
      libpathplan4 \
      libpcre3 \
      libpixman-1-0 \
      libpng16-16 \
      libraw15 \
      libsm6 \
      libstdc++6 \
      libthai0 \
      libtiff5 \
      libuuid1 \
      libwebp6 \
      libx11-6 \
      libxau6 \
      libxcb1 \
      libxcb-render0 \
      libxcb-shm0 \
      libxdmcp6 \
      libxdot4 \
      libxext6 \
      libxml2 \
      libxrender1 \
      zlib1g \
      jpegoptim
# install libjpeg8
RUN curl http://ftp.us.debian.org/debian/pool/main/libj/libjpeg8/libjpeg8_8d-1+deb7u1_amd64.deb > /tmp/libjpeg8_8d-1+deb7u1_amd64.deb \
      && dpkg -i /tmp/libjpeg8_8d-1+deb7u1_amd64.deb \
      && rm /tmp/libjpeg8_8d-1+deb7u1_amd64.deb
# copy image magick
COPY ./imagemagick-stretch-linux-x64/* /usr/lib/
# add ImageMagick 7 to ldconfig
RUN /sbin/ldconfig
# copy app
WORKDIR /app
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:80
COPY --from=build-env-ncimages /app/out ./
COPY ./docker/NCoreUtils.Images.WebService.runtimeconfig.json ./
ENTRYPOINT ["./NCoreUtils.Images.WebService"]