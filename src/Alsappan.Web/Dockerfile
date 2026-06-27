# syntax=docker/dockerfile:1

FROM node:24-alpine AS build
WORKDIR /src

ARG VITE_API_BASE_URL=/v1
ENV VITE_API_BASE_URL=$VITE_API_BASE_URL

COPY package.json package-lock.json ./
COPY src/Alsappan.Web/package.json src/Alsappan.Web/package.json

RUN npm ci

COPY src/Alsappan.Web src/Alsappan.Web
RUN npm run build --workspace src/Alsappan.Web

FROM nginx:1.29-alpine AS runtime

COPY src/Alsappan.Web/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /src/src/Alsappan.Web/dist /usr/share/nginx/html

EXPOSE 8080
