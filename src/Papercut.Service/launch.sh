#!/bin/bash

cmd="$1 >> $2 2>&1 &"
eval $cmd
wait