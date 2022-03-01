ARG BASE_IMAGE=amd64/alpine

FROM node:lts-alpine as builder
RUN apk add --update --no-cache patch
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm install -g typescript
RUN npm install -g pkg
RUN npm ci
COPY src/ ./src/
RUN tsc src/index.ts
ARG PKG_TARGET=node12-linuxstatic-x64
RUN pkg src/index.js --targets ${PKG_TARGET} --output /app/agentd-client


FROM $BASE_IMAGE as runtime
WORKDIR /app

ARG TINI_VERSION=v0.19.0
ARG TINI_BUILD=tini-static
RUN wget -O /tini https://github.com/krallin/tini/releases/download/${TINI_VERSION}/${TINI_BUILD} && chmod +x /tini

RUN addgroup -S agentd && adduser -S agentd -G agentd
USER agentd

COPY --from=builder /app/agentd-client /app/agentd-client
ENTRYPOINT ["/tini", "-g", "--", "/app/agentd-client"]