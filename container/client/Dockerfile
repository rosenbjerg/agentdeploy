ARG BASE_IMAGE=amd64/alpine

FROM node:lts-alpine as builder
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm install -g typescript
RUN npm install -g pkg
RUN npm ci
COPY src/ ./src/
RUN tsc src/index.ts
ARG PKG_TARGET=node8-alpine-x64
RUN pkg src/index.js --targets node8-alpine-x64 --output /app/agentd-client


FROM $BASE_IMAGE as runtime
RUN addgroup -S agentd && adduser -S agentd -G agentd
USER agentd
WORKDIR /app

ARG TINI_VERSION=v0.19.0
RUN wget -O /tini https://github.com/krallin/tini/releases/download/${TINI_VERSION}/tini-static && chmod +x /tini
ENTRYPOINT ["/tini", "-g", "--"]

COPY --from=builder /app/agentd-client /app/agentd-client
CMD /app/agentd-client