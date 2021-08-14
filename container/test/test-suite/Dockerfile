FROM mcr.microsoft.com/dotnet/sdk:5.0-alpine as builder
RUN apk add --update --no-cache icu-libs openssh-client sshpass nodejs
WORKDIR /app
CMD dotnet build --verbosity quiet server/AgentDeploy.sln && \
    echo "-- Running E2E suite: no target" && \
    dotnet test -c Debug --filter TestCategory=E2E-notarget --collect "XPlat Code Coverage" --logger GitHubActions --verbosity normal --no-build server/AgentDeploy.sln && \
    echo "-- Running E2E suite: local" && \
    dotnet test -c Debug --filter TestCategory=E2E-local --collect "XPlat Code Coverage" --logger GitHubActions --verbosity normal --no-build server/AgentDeploy.sln && \
    echo "-- Running E2E suite: sshpass" && \
    dotnet test -c Debug --filter TestCategory=E2E-sshpass --collect "XPlat Code Coverage" --logger GitHubActions --verbosity normal --no-build server/AgentDeploy.sln && \
    echo "-- Running E2E suite: privatekey" && \
    dotnet test -c Debug --filter TestCategory=E2E-privatekey --collect "XPlat Code Coverage" --logger GitHubActions --verbosity normal --no-build server/AgentDeploy.sln

