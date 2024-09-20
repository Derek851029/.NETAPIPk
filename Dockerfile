#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

# 基础映像
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build

# 设置工作目录
WORKDIR /app

# 复制.csproj文件并还原依赖项
COPY *.csproj ./
RUN dotnet restore

# 复制所有文件并生成应用程序
COPY . .
RUN dotnet publish -c Release -o out

# 第二个阶段，运行时映像
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS runtime

# 设置工作目录
WORKDIR /app

# 从build阶段复制生成的应用程序
COPY --from=build /app/out ./

# 设置环境变量
ENV ASPNETCORE_URLS=http://+:8080

# 复制React应用程序到wwwroot文件夹
COPY build ./wwwroot

# 暴露端口
EXPOSE 8080

# 启动应用程序
ENTRYPOINT ["dotnet", "PKApp.dll"]


#FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
#WORKDIR /app
#EXPOSE 80
#EXPOSE 443
#RUN apt-get update
#RUN apt-get install -y curl
#RUN apt-get install -y libpng-dev libjpeg-dev curl libxi6 build-essential libgl1-mesa-glx
#RUN curl -sL https://deb.nodesource.com/setup_lts.x | bash -
#RUN apt-get install -y nodejs
#
#FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
#RUN apt-get update
#RUN apt-get install -y curl
#RUN apt-get install -y libpng-dev libjpeg-dev curl libxi6 build-essential libgl1-mesa-glx
#RUN curl -sL https://deb.nodesource.com/setup_lts.x | bash -
#RUN apt-get install -y nodejs
#WORKDIR /src
#
#COPY ["PKApp.csproj", "PKApp/"]
#RUN dotnet restore "PKApp/PKApp.csproj"
#WORKDIR "/src/PKApp"
#COPY . .
#
#RUN dotnet build "PKApp.csproj" -c Release -o /app/build
#
#FROM build AS publish
#RUN dotnet publish "PKApp.csproj" -c Release -o /app/publish
#
#FROM node:16 AS build-web
#COPY ./../PKSystem/package.json /PKSystem/package.json
#COPY ./PKSystem/package-lock.json /PKSystem/package-lock.json
#WORKDIR /PKSystem
#RUN npm ci
#COPY ./PKSystem/ /PKSystem
#RUN npm run build
#
#FROM base AS final
#WORKDIR /app
#COPY --from=publish /app/publish .
#ENTRYPOINT ["dotnet", "PKApp.dll"]