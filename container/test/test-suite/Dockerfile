FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine as builder
RUN apk add --update --no-cache icu-libs openssh-client sshpass nodejs
WORKDIR /app
CMD dotnet build --verbosity quiet server/AgentDeploy.sln && \
    dotnet test -c Debug --collect "XPlat Code Coverage" --logger GitHubActions --verbosity normal --no-build server/AgentDeploy.sln

