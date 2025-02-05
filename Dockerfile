FROM mcr.microsoft.com/dotnet/sdk:7.0.102-alpine3.16 AS build
WORKDIR Authorization/

COPY src/Authorization ./Authorization
WORKDIR Authorization/

RUN dotnet build Altinn.Platform.Authorization.csproj -c Release -o /app_output
RUN dotnet publish Altinn.Platform.Authorization.csproj -c Release -o /app_output

FROM mcr.microsoft.com/dotnet/aspnet:7.0.2-alpine3.16 AS final
EXPOSE 5030
WORKDIR /app
COPY --from=build /app_output .

COPY src/Authorization/Migration ./Migration

# setup the user and group
# the user will have no password, using shell /bin/false and using the group dotnet
RUN addgroup -g 3000 dotnet && adduser -u 1000 -G dotnet -D -s /bin/false dotnet
# update permissions of files if neccessary before becoming dotnet user
USER dotnet
RUN mkdir /tmp/logtelemetry

ENTRYPOINT ["dotnet", "Altinn.Platform.Authorization.dll"]
