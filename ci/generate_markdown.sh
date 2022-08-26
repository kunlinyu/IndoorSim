#!/usr/bin/bash

CURRENT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}"  )" && pwd  )"

pandoc ../RELEASE.md -f markdown -o index.html --template=${CURRENT_DIR}/easy_template.html --toc --metadata pagetitle="IndoorSim release"
