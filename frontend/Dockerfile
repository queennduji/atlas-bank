# Build the static bundle. Vite inlines VITE_* env vars into the JS at build time (not
# runtime), so they're passed in as build ARGs and must be URLs the *browser* can reach —
# i.e. the host-published ports (localhost:5000, localhost:8080), not container hostnames.
FROM node:22-alpine AS build
WORKDIR /app

ARG VITE_GATEWAY_URL=http://localhost:5000
ARG VITE_KEYCLOAK_URL=http://localhost:8080
ARG VITE_KEYCLOAK_REALM=atlas-bank
ARG VITE_KEYCLOAK_CLIENT_ID=atlas-bank-app
ENV VITE_GATEWAY_URL=$VITE_GATEWAY_URL \
    VITE_KEYCLOAK_URL=$VITE_KEYCLOAK_URL \
    VITE_KEYCLOAK_REALM=$VITE_KEYCLOAK_REALM \
    VITE_KEYCLOAK_CLIENT_ID=$VITE_KEYCLOAK_CLIENT_ID

COPY package*.json ./
RUN npm ci
COPY . .
RUN npm run build

# Serve the static build.
FROM nginx:1.27-alpine AS final
COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
