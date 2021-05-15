ARG BASE_IMAGE=amd64/alpine

FROM mcr.microsoft.com/dotnet/sdk:5.0-alpine as builder
WORKDIR /app
COPY ./ /app/src
ARG DOTNET_RID=linux-musl-x64
RUN dotnet publish -r ${DOTNET_RID} /p:PublishTrimmed=true -o /app/build src/AgentDeploy.ExternalApi/AgentDeploy.ExternalApi.csproj



FROM $BASE_IMAGE as runtime
RUN apk add --update --no-cache icu-libs openssh sshpass
ARG CLAMAV=false
ENV CLAMAV=$CLAMAV
RUN if [ "$CLAMAV" = "true" ]; then apk add --update --no-cache clamav && freshclam; fi
EXPOSE 5000
ENV ASPNETCORE_URLS=http://*:5000
WORKDIR /app

ARG TINI_VERSION=v0.19.0
RUN wget -O /tini https://github.com/krallin/tini/releases/download/${TINI_VERSION}/tini-static && chmod +x /tini

RUN addgroup -S agentd && adduser -S agentd -G agentd
USER agentd

COPY --from=builder /app/build /app
ENTRYPOINT ["/tini", "-g", "--"]
CMD /app/AgentDeploy.ExternalApi