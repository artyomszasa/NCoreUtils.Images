FROM microsoft/dotnet:2.2-sdk AS build-env-ncimages
WORKDIR /app
COPY ./NuGet.Config ./
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
COPY ./NCoreUtils.Images.GoogleCloudStorage/*.fsproj ./NCoreUtils.Images.GoogleCloudStorage/
COPY ./NCoreUtils.Images.Internal/*.fsproj ./NCoreUtils.Images.Internal/
COPY ./NCoreUtils.Images.Internal.GoogleCloudStorage/*.fsproj ./NCoreUtils.Images.Internal.GoogleCloudStorage/
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
COPY ./NCoreUtils.Images.GoogleCloudStorage/*.fs ./NCoreUtils.Images.GoogleCloudStorage/
COPY ./NCoreUtils.Images.Internal/*.fs ./NCoreUtils.Images.Internal/
COPY ./NCoreUtils.Images.Internal.GoogleCloudStorage/*.fs ./NCoreUtils.Images.Internal.GoogleCloudStorage/
RUN dotnet publish NCoreUtils.Images.WebService/NCoreUtils.Images.WebService.fsproj --no-restore -c Release -r linux-x64 -o /app/out

# RUNTIME IMAGE

FROM microsoft/dotnet:2.2-runtime-deps
RUN apt-get update && DEBIAN_FRONTEND=noninteractive apt-get install -y apt-utils
# install imagemagick dependencies
RUN apt-get update \
    && DEBIAN_FRONTEND=noninteractive apt-get install -y --no-install-recommends \
      curl \
      jpegoptim
# copy app
WORKDIR /app
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:80
COPY --from=build-env-ncimages /app/out ./
COPY ./docker/NCoreUtils.Images.WebService.runtimeconfig.json ./
ENTRYPOINT ["./NCoreUtils.Images.WebService"]