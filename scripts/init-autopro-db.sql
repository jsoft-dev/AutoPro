-- Run once as a superuser (e.g. postgres) in psql or pgAdmin Query Tool.
-- Password must match ConnectionStrings:DefaultConnection in src/AutoPro.Api/appsettings.json (and docker-compose.yml).

CREATE ROLE autopro WITH LOGIN PASSWORD 'autopro_dev';
CREATE DATABASE autopro OWNER autopro;
GRANT ALL PRIVILEGES ON DATABASE autopro TO autopro;
