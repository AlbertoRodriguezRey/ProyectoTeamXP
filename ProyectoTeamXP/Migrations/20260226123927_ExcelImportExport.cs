using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProyectoTeamXP.Migrations
{
    /// <inheritdoc />
    public partial class ExcelImportExport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RecursosFAQ",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Categoria = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Titulo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UrlVideo1 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UrlVideo2 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecursosFAQ", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UsuariosSeguridad",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PasswordHash = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    PasswordSalt = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    PasswordPlain_SOLO_TESTING = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RolId = table.Column<int>(type: "int", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UltimoAcceso = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    Eliminado = table.Column<bool>(type: "bit", nullable: false),
                    FechaEliminacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UsuarioEliminacion = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsuariosSeguridad", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UsuariosSeguridad_Roles_RolId",
                        column: x => x.RolId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClientesPerfil",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioSeguridadId = table.Column<int>(type: "int", nullable: false),
                    NombreCompleto = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Edad = table.Column<int>(type: "int", nullable: true),
                    PesoInicial = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    FechaInicioPrograma = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Objetivos = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ObjetivoPasos = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ObjetivoCardio = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Suplementacion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TerminosAceptados = table.Column<bool>(type: "bit", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UsuarioModificacion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Eliminado = table.Column<bool>(type: "bit", nullable: false),
                    FechaEliminacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UsuarioEliminacion = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientesPerfil", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClientesPerfil_UsuariosSeguridad_UsuarioSeguridadId",
                        column: x => x.UsuarioSeguridadId,
                        principalTable: "UsuariosSeguridad",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ArchivosProgreso",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClienteId = table.Column<int>(type: "int", nullable: false),
                    FechaSubida = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NombreOriginal = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RutaServidor = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TipoVista = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TipoArchivo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Eliminado = table.Column<bool>(type: "bit", nullable: false),
                    FechaEliminacion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArchivosProgreso", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ArchivosProgreso_ClientesPerfil_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "ClientesPerfil",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClienteMacros",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClienteId = table.Column<int>(type: "int", nullable: false),
                    TipoDia = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Proteina = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    Grasa = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    Carbohidratos = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    Calorias = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClienteMacros", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClienteMacros_ClientesPerfil_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "ClientesPerfil",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MedidasCorporales",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClienteId = table.Column<int>(type: "int", nullable: false),
                    NumeroSemana = table.Column<int>(type: "int", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Pecho = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    Brazo = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    CinturaSobreOmbligo = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    CinturaOmbligo = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    CinturaBajoOmbligo = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    Cadera = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    Muslos = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedidasCorporales", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MedidasCorporales_ClientesPerfil_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "ClientesPerfil",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NotasSemanalesCoach",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClienteId = table.Column<int>(type: "int", nullable: false),
                    Anio = table.Column<int>(type: "int", nullable: false),
                    SemanaNumero = table.Column<int>(type: "int", nullable: false),
                    CambiosAlteraciones = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NotasNutricion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NotasCardio = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NotasEntrenamiento = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotasSemanalesCoach", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotasSemanalesCoach_ClientesPerfil_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "ClientesPerfil",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlanNutricional",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClienteId = table.Column<int>(type: "int", nullable: false),
                    Comida = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OrdenComida = table.Column<int>(type: "int", nullable: true),
                    Alimento = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Cantidad = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Sustituto = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CantidadSustituto = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanNutricional", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlanNutricional_ClientesPerfil_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "ClientesPerfil",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RevisionesFeedback",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClienteId = table.Column<int>(type: "int", nullable: false),
                    FechaRevision = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExitosSemana = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FeedbackEntreno = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FeedbackNutricion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FeedbackFisico = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FeedbackAnimo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FeedbackApoyo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProximasSemanas = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FechaCompletado = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RevisionesFeedback", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RevisionesFeedback_ClientesPerfil_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "ClientesPerfil",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RutinaEjercicios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClienteId = table.Column<int>(type: "int", nullable: false),
                    DiaRutina = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OrdenEjercicio = table.Column<int>(type: "int", nullable: true),
                    GrupoMuscular = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NombreEjercicio = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LinkVideo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NotasTecnicas = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SeriesObjetivo = table.Column<int>(type: "int", nullable: true),
                    RepsRango = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EsfuerzoObjetivo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Eliminado = table.Column<bool>(type: "bit", nullable: false),
                    FechaEliminacion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RutinaEjercicios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RutinaEjercicios_ClientesPerfil_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "ClientesPerfil",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SeguimientoDiario",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClienteId = table.Column<int>(type: "int", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NumeroSemana = table.Column<int>(type: "int", nullable: true),
                    Peso = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    HoraPesaje = table.Column<TimeSpan>(type: "time", nullable: true),
                    Proteina = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    Grasa = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    Carbohidratos = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    TotalCalorias = table.Column<int>(type: "int", nullable: true),
                    DiaEntreno = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RendimientoSesion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HorasSueno = table.Column<decimal>(type: "decimal(4,2)", precision: 4, scale: 2, nullable: true),
                    CalidadSueno = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Apetito = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NivelEstres = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PasosRealizados = table.Column<int>(type: "int", nullable: true),
                    DuracionCardio = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EntrenoRealizado = table.Column<bool>(type: "bit", nullable: false),
                    CardioRealizado = table.Column<bool>(type: "bit", nullable: false),
                    Notas = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeguimientoDiario", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SeguimientoDiario_ClientesPerfil_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "ClientesPerfil",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProgresionSeries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RutinaEjercicioId = table.Column<int>(type: "int", nullable: false),
                    NumeroSemana = table.Column<int>(type: "int", nullable: false),
                    NumeroSerie = table.Column<int>(type: "int", nullable: false),
                    CargaKg = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: true),
                    RepsRealizadas = table.Column<int>(type: "int", nullable: true),
                    RIRRealizado = table.Column<int>(type: "int", nullable: true),
                    Comentarios = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgresionSeries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProgresionSeries_RutinaEjercicios_RutinaEjercicioId",
                        column: x => x.RutinaEjercicioId,
                        principalTable: "RutinaEjercicios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ArchivosProgreso_ClienteId",
                table: "ArchivosProgreso",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_ClienteMacros_ClienteId",
                table: "ClienteMacros",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientesPerfil_UsuarioSeguridadId",
                table: "ClientesPerfil",
                column: "UsuarioSeguridadId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MedidasCorporales_ClienteId",
                table: "MedidasCorporales",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_NotasSemanalesCoach_ClienteId",
                table: "NotasSemanalesCoach",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanNutricional_ClienteId",
                table: "PlanNutricional",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_ProgresionSeries_RutinaEjercicioId",
                table: "ProgresionSeries",
                column: "RutinaEjercicioId");

            migrationBuilder.CreateIndex(
                name: "IX_RevisionesFeedback_ClienteId",
                table: "RevisionesFeedback",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_RutinaEjercicios_ClienteId",
                table: "RutinaEjercicios",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_SeguimientoDiario_ClienteId",
                table: "SeguimientoDiario",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_UsuariosSeguridad_RolId",
                table: "UsuariosSeguridad",
                column: "RolId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ArchivosProgreso");

            migrationBuilder.DropTable(
                name: "ClienteMacros");

            migrationBuilder.DropTable(
                name: "MedidasCorporales");

            migrationBuilder.DropTable(
                name: "NotasSemanalesCoach");

            migrationBuilder.DropTable(
                name: "PlanNutricional");

            migrationBuilder.DropTable(
                name: "ProgresionSeries");

            migrationBuilder.DropTable(
                name: "RecursosFAQ");

            migrationBuilder.DropTable(
                name: "RevisionesFeedback");

            migrationBuilder.DropTable(
                name: "SeguimientoDiario");

            migrationBuilder.DropTable(
                name: "RutinaEjercicios");

            migrationBuilder.DropTable(
                name: "ClientesPerfil");

            migrationBuilder.DropTable(
                name: "UsuariosSeguridad");

            migrationBuilder.DropTable(
                name: "Roles");
        }
    }
}
