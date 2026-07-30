using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanzasInteligentes.Infraestructura.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class M0015_PermisosRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $permisos$
                DECLARE
                  esquema text;
                BEGIN
                  IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'finanzas_api') THEN
                    FOREACH esquema IN ARRAY ARRAY[
                      'identidad', 'finanzas', 'infra', 'analitica', 'documentos',
                      'familias', 'suscripciones', 'seguridad', 'auditoria'
                    ]
                    LOOP
                      EXECUTE format('GRANT USAGE ON SCHEMA %I TO finanzas_api', esquema);
                    END LOOP;

                    GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA identidad TO finanzas_api;
                    GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA finanzas TO finanzas_api;
                    GRANT SELECT, INSERT, UPDATE ON ALL TABLES IN SCHEMA infra TO finanzas_api;
                    GRANT SELECT, INSERT, UPDATE ON ALL TABLES IN SCHEMA analitica TO finanzas_api;
                    GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA documentos TO finanzas_api;
                    GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA familias TO finanzas_api;
                    GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA suscripciones TO finanzas_api;
                    GRANT SELECT, INSERT, UPDATE ON ALL TABLES IN SCHEMA seguridad TO finanzas_api;
                    GRANT SELECT, INSERT ON ALL TABLES IN SCHEMA auditoria TO finanzas_api;

                    GRANT USAGE, SELECT, UPDATE ON ALL SEQUENCES IN SCHEMA
                      identidad, finanzas, infra, analitica, documentos,
                      familias, suscripciones, seguridad, auditoria TO finanzas_api;

                    ALTER DEFAULT PRIVILEGES IN SCHEMA identidad
                      GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO finanzas_api;
                    ALTER DEFAULT PRIVILEGES IN SCHEMA finanzas
                      GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO finanzas_api;
                    ALTER DEFAULT PRIVILEGES IN SCHEMA infra
                      GRANT SELECT, INSERT, UPDATE ON TABLES TO finanzas_api;
                    ALTER DEFAULT PRIVILEGES IN SCHEMA analitica
                      GRANT SELECT, INSERT, UPDATE ON TABLES TO finanzas_api;
                    ALTER DEFAULT PRIVILEGES IN SCHEMA documentos
                      GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO finanzas_api;
                    ALTER DEFAULT PRIVILEGES IN SCHEMA familias
                      GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO finanzas_api;
                    ALTER DEFAULT PRIVILEGES IN SCHEMA suscripciones
                      GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO finanzas_api;
                    ALTER DEFAULT PRIVILEGES IN SCHEMA seguridad
                      GRANT SELECT, INSERT, UPDATE ON TABLES TO finanzas_api;
                    ALTER DEFAULT PRIVILEGES IN SCHEMA auditoria
                      GRANT SELECT, INSERT ON TABLES TO finanzas_api;
                    ALTER DEFAULT PRIVILEGES
                      GRANT USAGE, SELECT, UPDATE ON SEQUENCES TO finanzas_api;
                  END IF;

                  IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'finanzas_worker') THEN
                    FOREACH esquema IN ARRAY ARRAY[
                      'identidad', 'finanzas', 'infra', 'analitica', 'documentos', 'familias'
                    ]
                    LOOP
                      EXECUTE format('GRANT USAGE ON SCHEMA %I TO finanzas_worker', esquema);
                    END LOOP;

                    GRANT SELECT, UPDATE ON ALL TABLES IN SCHEMA identidad TO finanzas_worker;
                    GRANT SELECT, INSERT, UPDATE ON ALL TABLES IN SCHEMA finanzas TO finanzas_worker;
                    GRANT SELECT, INSERT, UPDATE ON ALL TABLES IN SCHEMA infra TO finanzas_worker;
                    GRANT SELECT, INSERT, UPDATE ON ALL TABLES IN SCHEMA analitica TO finanzas_worker;
                    GRANT SELECT, INSERT, UPDATE ON ALL TABLES IN SCHEMA documentos TO finanzas_worker;
                    GRANT SELECT, UPDATE ON ALL TABLES IN SCHEMA familias TO finanzas_worker;

                    GRANT USAGE, SELECT, UPDATE ON ALL SEQUENCES IN SCHEMA
                      finanzas, infra, analitica, documentos TO finanzas_worker;

                    ALTER DEFAULT PRIVILEGES IN SCHEMA identidad
                      GRANT SELECT, UPDATE ON TABLES TO finanzas_worker;
                    ALTER DEFAULT PRIVILEGES IN SCHEMA finanzas
                      GRANT SELECT, INSERT, UPDATE ON TABLES TO finanzas_worker;
                    ALTER DEFAULT PRIVILEGES IN SCHEMA infra
                      GRANT SELECT, INSERT, UPDATE ON TABLES TO finanzas_worker;
                    ALTER DEFAULT PRIVILEGES IN SCHEMA analitica
                      GRANT SELECT, INSERT, UPDATE ON TABLES TO finanzas_worker;
                    ALTER DEFAULT PRIVILEGES IN SCHEMA documentos
                      GRANT SELECT, INSERT, UPDATE ON TABLES TO finanzas_worker;
                    ALTER DEFAULT PRIVILEGES IN SCHEMA familias
                      GRANT SELECT, UPDATE ON TABLES TO finanzas_worker;
                    ALTER DEFAULT PRIVILEGES
                      GRANT USAGE, SELECT, UPDATE ON SEQUENCES TO finanzas_worker;
                  END IF;
                END
                $permisos$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $permisos$
                DECLARE
                  esquema text;
                BEGIN
                  IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'finanzas_api') THEN
                    ALTER DEFAULT PRIVILEGES REVOKE ALL ON SEQUENCES FROM finanzas_api;
                    FOREACH esquema IN ARRAY ARRAY[
                      'identidad', 'finanzas', 'infra', 'analitica', 'documentos',
                      'familias', 'suscripciones', 'seguridad', 'auditoria'
                    ]
                    LOOP
                      EXECUTE format(
                        'ALTER DEFAULT PRIVILEGES IN SCHEMA %I REVOKE ALL ON TABLES FROM finanzas_api',
                        esquema);
                      EXECUTE format(
                        'REVOKE ALL PRIVILEGES ON ALL TABLES IN SCHEMA %I FROM finanzas_api',
                        esquema);
                      EXECUTE format(
                        'REVOKE ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA %I FROM finanzas_api',
                        esquema);
                      EXECUTE format('REVOKE USAGE ON SCHEMA %I FROM finanzas_api', esquema);
                    END LOOP;

                    GRANT USAGE ON SCHEMA identidad, finanzas, infra TO finanzas_api;
                    GRANT SELECT, INSERT, UPDATE, DELETE
                      ON ALL TABLES IN SCHEMA identidad, finanzas TO finanzas_api;
                    GRANT SELECT, INSERT, UPDATE ON ALL TABLES IN SCHEMA infra TO finanzas_api;
                  END IF;

                  IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'finanzas_worker') THEN
                    ALTER DEFAULT PRIVILEGES REVOKE ALL ON SEQUENCES FROM finanzas_worker;
                    FOREACH esquema IN ARRAY ARRAY[
                      'identidad', 'finanzas', 'infra', 'analitica', 'documentos', 'familias'
                    ]
                    LOOP
                      EXECUTE format(
                        'ALTER DEFAULT PRIVILEGES IN SCHEMA %I REVOKE ALL ON TABLES FROM finanzas_worker',
                        esquema);
                      EXECUTE format(
                        'REVOKE ALL PRIVILEGES ON ALL TABLES IN SCHEMA %I FROM finanzas_worker',
                        esquema);
                      EXECUTE format(
                        'REVOKE ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA %I FROM finanzas_worker',
                        esquema);
                      EXECUTE format('REVOKE USAGE ON SCHEMA %I FROM finanzas_worker', esquema);
                    END LOOP;

                    GRANT USAGE ON SCHEMA identidad, finanzas, infra TO finanzas_worker;
                    GRANT SELECT ON ALL TABLES IN SCHEMA identidad, finanzas TO finanzas_worker;
                    GRANT SELECT, INSERT, UPDATE ON ALL TABLES IN SCHEMA infra TO finanzas_worker;
                  END IF;
                END
                $permisos$;
                """);
        }
    }
}