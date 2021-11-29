#!/bin/bash

cd ./server

USERINFO=$(curl -fs https://api.mojang.com/users/profiles/minecraft/${MC_USER})

if [ $? -ne 0 ]; then
   echo "failed to retrieve userdata for user \"${MC_USER}\""
   exit 2
fi

echo "$USERINFO" | jq '{uuid: (.id[0:8]+"-"+.id[8:12]+"-"+.id[12:16]+"-"+.id[16:20]+"-"+.id[20:32]) , name: .name} | . + {"level": 4,"bypassesPlayerLimit": false} | [.]' > ./ops.json

code-server &

java -Xmx2G -cp 'paper-1.17.1-384.jar:./libs/*' -javaagent:paper-1.17.1-384.jar io.papermc.paperclip.Paperclip
