using ConsultasPlan.Models;

namespace ConsultasPlan.Services.Interfaces
{
    public interface IConsultaService
    {
        Task<Consulta> CrearAsync(Consulta consulta);
    }
}