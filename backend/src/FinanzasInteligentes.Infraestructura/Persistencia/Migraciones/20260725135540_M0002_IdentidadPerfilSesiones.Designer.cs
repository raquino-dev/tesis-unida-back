using FinanzasInteligentes.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanzasInteligentes.Infraestructura.Persistencia.Migraciones;

[DbContext(typeof(FinanzasDbContext))]
[Migration("20260725135540_M0002_IdentidadPerfilSesiones")]
partial class M0002_IdentidadPerfilSesiones
{
    protected override void BuildTargetModel(ModelBuilder modelBuilder)
    {
    }
}
