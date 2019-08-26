FROM alpine:3.10

RUN apk add --update python py-pip jq && \
    pip install yq
    
ADD entrypoint.sh ./

ENTRYPOINT ["/entrypoint.sh"]