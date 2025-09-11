#!/bin/bash

WORKSPACE=..
SERVER_ROOT=../../Luban/Gen
LUBAN_DLL=./Tools/Luban/Luban.dll
CONF_ROOT=.

dotnet $LUBAN_DLL \
    -t all \
    -d json \
    -c cs-newtonsoft-json \
    --conf $CONF_ROOT/luban.conf \
    -x outputCodeDir=$SERVER_ROOT \
    -x outputDataDir=../Data