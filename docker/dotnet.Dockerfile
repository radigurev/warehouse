FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG PROJECT_PATH
ARG ASSEMBLY_NAME
WORKDIR /src

# Copy all csproj files for restore
COPY src/Warehouse.Common/Warehouse.Common.csproj Warehouse.Common/
COPY src/Warehouse.Infrastructure/Warehouse.Infrastructure.csproj Warehouse.Infrastructure/
COPY src/Warehouse.GenericFiltering/Warehouse.GenericFiltering.csproj Warehouse.GenericFiltering/
COPY src/Warehouse.Mapping/Warehouse.Mapping.csproj Warehouse.Mapping/
COPY src/Warehouse.ServiceModel/Warehouse.ServiceModel.csproj Warehouse.ServiceModel/
COPY src/Databases/Warehouse.Auth.DBModel/Warehouse.Auth.DBModel.csproj Databases/Warehouse.Auth.DBModel/
COPY src/Databases/Warehouse.Customers.DBModel/Warehouse.Customers.DBModel.csproj Databases/Warehouse.Customers.DBModel/
COPY src/Databases/Warehouse.Inventory.DBModel/Warehouse.Inventory.DBModel.csproj Databases/Warehouse.Inventory.DBModel/
COPY src/Databases/Warehouse.Purchasing.DBModel/Warehouse.Purchasing.DBModel.csproj Databases/Warehouse.Purchasing.DBModel/
COPY src/Databases/Warehouse.Fulfillment.DBModel/Warehouse.Fulfillment.DBModel.csproj Databases/Warehouse.Fulfillment.DBModel/
COPY src/Gateway/Warehouse.Gateway/Warehouse.Gateway.csproj Gateway/Warehouse.Gateway/
COPY src/Interfaces/Auth/Warehouse.Auth.API/Warehouse.Auth.API.csproj Interfaces/Auth/Warehouse.Auth.API/
COPY src/Interfaces/Customers/Warehouse.Customers.API/Warehouse.Customers.API.csproj Interfaces/Customers/Warehouse.Customers.API/
COPY src/Interfaces/Inventory/Warehouse.Inventory.API/Warehouse.Inventory.API.csproj Interfaces/Inventory/Warehouse.Inventory.API/
COPY src/Interfaces/Purchasing/Warehouse.Purchasing.API/Warehouse.Purchasing.API.csproj Interfaces/Purchasing/Warehouse.Purchasing.API/
COPY src/Interfaces/Fulfillment/Warehouse.Fulfillment.API/Warehouse.Fulfillment.API.csproj Interfaces/Fulfillment/Warehouse.Fulfillment.API/

RUN dotnet restore ${PROJECT_PATH}

COPY src/ .
RUN dotnet publish ${PROJECT_PATH} -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
ARG ASSEMBLY_NAME
ENV ASSEMBLY_NAME=${ASSEMBLY_NAME}
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT dotnet ${ASSEMBLY_NAME}
