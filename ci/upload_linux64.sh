#!/bin/bash

if [ $# -eq 0 ]
then
  echo "Usage upload.sh IndoorSim-StandaloneLinux64-V0.0.0"
fi

RELEASEFILE=$1
echo $RELEASEFILE

aws s3 cp $RELEASEFILE s3://indoorsim/$RELEASEFILE
