# Build stage
FROM node:18-alpine AS build
WORKDIR /app

# Copy package files
COPY BoltTickets.Frontend/package*.json ./

# Install dependencies
RUN npm ci

# Copy source code
COPY BoltTickets.Frontend/ ./

# Build the application
RUN npm run build

# Production stage
FROM nginx:alpine
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]