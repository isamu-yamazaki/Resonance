FROM ubuntu:24.04

WORKDIR /game
COPY ./Builds/Default/Linux/ .
RUN chmod +x ./ResonanceServer.x86_64 && \
    groupadd -g 2000 gameuser && useradd -g 2000 -u 2000 -m gameuser && \
    chown -R gameuser:gameuser /game
USER gameuser

# What command should it run when you start the container?
# This is just a linux command that runs "build.x86_64" in the root directory "."
# Change that to whatever you named your exported build
CMD ["./ResonanceServer.x86_64"]
