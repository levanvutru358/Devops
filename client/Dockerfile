# syntax=docker/dockerfile:1.7

###########
# BUILD
###########
FROM node:20-alpine AS build
WORKDIR /app

# Cache deps
COPY package*.json ./
RUN npm ci

# Copy source & build
COPY . .
ARG VITE_API_URL=http://localhost:5193
ENV VITE_API_URL=${VITE_API_URL}
RUN npm run build

###########
# RUNTIME
###########
FROM nginx:1.27-alpine AS final
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
