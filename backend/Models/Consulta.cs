using ConsultasPlan.Exceptions;

namespace ConsultasPlan.Models;

public class Consulta
{
    public int Id { get; set; }
    public int? AfiliadoId { get; set; }
    public Afiliado? Afiliado { get; set; }
    public int ProfesionalId { get; set; }
    public Profesional? Profesional { get; set; }
    public DateTime FechaHora { get; set; }
    public EstadoConsulta Estado { get; set; }
    public ModalidadConsulta Modalidad { get; set; }
    public DateTime FechaCreacion { get; set; }
    public string Motivo { get; set; } = string.Empty;

    private static readonly Dictionary<EstadoConsulta, EstadoConsulta[]> TransicionesValidas = new()
    {
        [EstadoConsulta.Pendiente] = new[] { EstadoConsulta.Confirmado, EstadoConsulta.Cancelado, EstadoConsulta.Atendido, EstadoConsulta.Inasistencia },
        [EstadoConsulta.Confirmado] = new[] { EstadoConsulta.Cancelado, EstadoConsulta.Atendido, EstadoConsulta.Inasistencia },
        [EstadoConsulta.Cancelado] = Array.Empty<EstadoConsulta>(),
        [EstadoConsulta.Atendido] = Array.Empty<EstadoConsulta>(),
        [EstadoConsulta.Inasistencia] = Array.Empty<EstadoConsulta>(),
    };

    public void ValidarTransicion(EstadoConsulta nuevo)
    {
        if (Estado == nuevo)
            throw new BusinessRuleException($"La consulta ya está en estado {Estado}.");

        if (!TransicionesValidas[Estado].Contains(nuevo))
            throw new BusinessRuleException($"No se puede pasar una consulta de {Estado} a {nuevo}.");
    }

}
