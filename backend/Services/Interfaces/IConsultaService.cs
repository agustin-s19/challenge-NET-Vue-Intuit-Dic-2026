using ConsultasPlan.Models;

namespace ConsultasPlan.Services.Interfaces
{
    public interface IConsultaService
    {
        Task<Consulta> CrearAsync(Consulta consulta);
        Task<Consulta> ConfirmarAsync(int id);
        Task<Consulta> CancelarAsync(int id);
        Task<Consulta> RegistrarAtencionAsync(int id);
        Task<Consulta> MarcarInasistenciaAsync(int id);
        Task<Consulta> CambiarEstadoAsync(int id, EstadoConsulta nuevo);
    }
}