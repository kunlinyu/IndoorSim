#!/bin/bash

if [ $# -eq 0 ]
then
  echo "Usage upload.sh IndoorSim-Release-V0.0.0"
fi

if [ -d "$1" ]
then
  RELEASEPATH=$1
else
  echo "Directory don't exist"
  exit
fi

aws s3 cp index.html s3://indoorsim/index.html

echo $RELEASEPATH

aws s3 cp $RELEASEPATH/index.html s3://indoorsim/$RELEASEPATH/index.html

aws s3 cp --recursive $RELEASEPATH/TemplateData s3://indoorsim/$RELEASEPATH/TemplateData
aws s3 cp --recursive $RELEASEPATH/StreamingAssets s3://indoorsim/$RELEASEPATH/StreamingAssets

aws s3 cp $RELEASEPATH/Build/$RELEASEPATH.data         s3://indoorsim/$RELEASEPATH/Build/$RELEASEPATH.data
aws s3 cp $RELEASEPATH/Build/$RELEASEPATH.framework.js s3://indoorsim/$RELEASEPATH/Build/$RELEASEPATH.framework.js --content-type=application/javascript
aws s3 cp $RELEASEPATH/Build/$RELEASEPATH.loader.js    s3://indoorsim/$RELEASEPATH/Build/$RELEASEPATH.loader.js    --content-type=application/javascript
aws s3 cp $RELEASEPATH/Build/$RELEASEPATH.wasm         s3://indoorsim/$RELEASEPATH/Build/$RELEASEPATH.wasm         --content-type=application/wasm
