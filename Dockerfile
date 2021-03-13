ARG BASE_IMAGE=amd64/alpine

FROM mcr.microsoft.com/dotnet/sdk:5.0-alpine as builder
WORKDIR /app
COPY ./src /app/src
ARG DOTNET_RID=linux-musl-x64
RUN dotnet publish -r ${DOTNET_RID} /p:PublishTrimmed=true -o /app/build src/AgentDeploy.ExternalApi/AgentDeploy.ExternalApi.csproj
COPY ./src/AgentDeploy.ExternalApi/appsettings.json /app/appsettings.json


FROM $BASE_IMAGE as runtime
RUN apk add --update --no-cache icu-libs sshpass

ARG TINI_VERSION=v0.19.0
RUN wget -O /tini https://github.com/krallin/tini/releases/download/${TINI_VERSION}/tini-static && chmod +x /tini
ENTRYPOINT ["/tini", "-g", "--"]

RUN addgroup -S appgroup && adduser -S appuser -G appgroup
USER appuser

EXPOSE 5000
ENV ASPNETCORE_URLS=http://*:5000
COPY --from=builder /app/build /app
CMD /app/AgentDeploy.ExternalApi