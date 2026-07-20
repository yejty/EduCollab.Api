FROM node:22-bookworm-slim AS builder

ENV NODE_ENV=production
ENV CI=1

WORKDIR /app

RUN apt-get update \
  && apt-get install -y --no-install-recommends ca-certificates \
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
COPY smoke-test.html ./smoke-test.html

ENV COLLAB_PORT=2567
ENV COLLAB_BIND_ADDR=0.0.0.0

EXPOSE 2567

CMD ["node", "dist/index.js"]
