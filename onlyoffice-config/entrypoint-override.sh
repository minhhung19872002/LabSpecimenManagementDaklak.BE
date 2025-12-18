#!/bin/bash

# Copy local.json from mounted location to actual config (so OnlyOffice can modify it)
if [ -f /onlyoffice-config/local.json ]; then
    cp /onlyoffice-config/local.json /etc/onlyoffice/documentserver/local.json
    chmod 644 /etc/onlyoffice/documentserver/local.json
fi

# Override private IP settings in default.json
sed -i 's/"allowPrivateIPAddress": false/"allowPrivateIPAddress": true/g' /etc/onlyoffice/documentserver/default.json
sed -i 's/"allowMetaIPAddress": false/"allowMetaIPAddress": true/g' /etc/onlyoffice/documentserver/default.json

# Run the original entrypoint
exec /app/ds/run-document-server.sh
