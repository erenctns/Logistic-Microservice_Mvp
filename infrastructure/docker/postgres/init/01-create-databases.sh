#!/bin/sh
# ==========================================================
#  Database-per-service: her servis icin ayri veritabani.
#
#  Bu script YALNIZCA postgres veri dizini bosken calisir
#  (yani ilk "docker compose up" veya "down -v" sonrasi).
#  Sonraki aciliste entrypoint burayi hic okumaz.
#
#  POSIX sh: alpine imajinda /bin/sh = ash, bash yok sayilir.
# ==========================================================
set -eu

create_database() {
    db_name="$1"

    # Idempotent: ayni script iki kez calissa bile patlamaz.
    exists=$(psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname postgres \
        -tAc "SELECT 1 FROM pg_database WHERE datname = '$db_name'")

    if [ "$exists" = "1" ]; then
        echo "[init]  = $db_name zaten var, atlandi"
    else
        psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname postgres \
            -c "CREATE DATABASE \"$db_name\""
        echo "[init]  + $db_name olusturuldu"
    fi
}

echo "[init] database-per-service: veritabanlari hazirlaniyor"

for db in "$AUTH_DB" "$ORDER_DB" "$SHIPMENT_DB" "$COURIER_DB" "$DELIVERY_DB" "$NOTIFICATION_DB"; do
    create_database "$db"
done

echo "[init] tamamlandi"
