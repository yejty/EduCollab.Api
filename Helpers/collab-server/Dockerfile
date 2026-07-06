# Multi-stage Node.js build. better-sqlite3 needs a C++ toolchain at install time;
# we install it in the builder stage and ship the prebuilt module.
FROM node:22-bookworm-slim AS builder

ENV NODE_ENV=production
ENV CI=1

WORKDIR /app

# build deps for native modules (better-sqlite3)
RUN apt-get update \
  && apt-get install -y --no-install-recommends \
    python3 \
    make \
    g++ \
    ca-certificates \
  && rm -rf /var/lib/apt/lists/*

COPY package.json package-lock.json ./
COPY tsconfig.json ./
RUN npm ci --include=dev --no-audit --no-fund

COPY src ./src
RUN npm run build \
  && npm prune --omit=dev


FROM node:22-bookworm-slim AS runtime

ENV NODE_ENV=production

WORKDIR /app

COPY --from=builder /app/node_modules ./node_modules
COPY --from=builder /app/dist ./dist
COPY package.json ./

ENV COLLAB_PORT=2567
ENV COLLAB_BIND_ADDR=0.0.0.0
ENV COLLAB_DB_PATH=/var/collab-data/collab.sqlite

RUN mkdir -p /var/collab-data && chmod 0755 /var/collab-data

EXPOSE 2567

CMD ["node", "dist/index.js"]
