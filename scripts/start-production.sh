#!/usr/bin/env bash
source "$(dirname "$0")/production-common.sh"
require_production_env
compose up -d --remove-orphans
