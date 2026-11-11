#!/bin/sh
set -e

mkdir -p /app/App_Data/Content/scenes \
         /app/App_Data/Content/assets \
         /app/App_Data/UserPreferences \
         /app/App_Data/WorkspaceThumbnails

if [ "$(id -u)" = "0" ]; then
  app_uid="${APP_UID:-1654}"
  chown -R "$app_uid:$app_uid" /app/App_Data
  exec gosu "$app_uid" "$@"
fi

exec "$@"
