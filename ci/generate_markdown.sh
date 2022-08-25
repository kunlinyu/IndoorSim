#!/usr/bin/bash

pandoc ../RELEASE.md -f markdown -o index.html --template=${HOME}/.pandoc/templates/easy_template.html --toc --metadata pagetitle="IndoorSim release"
