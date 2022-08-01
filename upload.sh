#!/bin/bash
aws s3 cp index.html s3://indoorsim/index.html
aws s3 cp --recursive ./TemplateData s3://indoorsim/TemplateData
aws s3 cp --recursive ./StreamingAssets s3://indoorsim/StreamingAssets

aws s3 cp Build/IndoorSim-release.data.gz         s3://indoorsim/Build/IndoorSim-release.data.gz         --content-encoding=gzip
aws s3 cp Build/IndoorSim-release.framework.js.gz s3://indoorsim/Build/IndoorSim-release.framework.js.gz --content-encoding=gzip --content-type=application/javascript
aws s3 cp Build/IndoorSim-release.loader.js       s3://indoorsim/Build/IndoorSim-release.loader.js                               --content-type=application/javascript
aws s3 cp Build/IndoorSim-release.wasm.gz         s3://indoorsim/Build/IndoorSim-release.wasm.gz         --content-encoding=gzip --content-type=application/wasm
