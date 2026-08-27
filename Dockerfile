FROM ubuntu:24.04

WORKDIR /game
COPY ./Builds/Default/Linux/ .
RUN apt-get update && \
    apt-get install -y --no-install-recommends ca-certificates && \
    update-ca-certificates && \
    rm -rf /var/lib/apt/lists/* && \
    rm -rf ./Resonance_BurstDebugInformation_DoNotShip && \
    chmod +x ./ResonanceServer.x86_64 && \
    groupadd -g 2000 gameuser && useradd -g 2000 -u 2000 -m gameuser && \
    chown -R gameuser:gameuser /game
USER gameuser

CMD ["./ResonanceServer.x86_64"]
