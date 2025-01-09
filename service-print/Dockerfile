FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /usr/src/app

COPY . .

RUN dotnet restore "02-ReceiveEvents.csproj"
RUN dotnet build "02-ReceiveEvents.csproj" -c Release -o /usr/src/app/build
RUN dotnet publish "02-ReceiveEvents.csproj" -c Release -o /usr/src/app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime

WORKDIR /usr/src/app

COPY --from=build /usr/src/app/publish .

EXPOSE 80

RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser
USER appuser

ENTRYPOINT ["dotnet", "02-ReceiveEvents.dll"]
