using ConsultasPlan.Models;

namespace ConsultasPlan.Services.Interfaces
{
    public interface IAfiliadoService
    {
        Task<List<Afiliado>> GetAllAsync(bool incluirInactivos = false);
        Task<Afiliado> GetByIdAsync(int id);
        Task<Afiliado> DesbloquearAsync(int id);
        Task EliminarAsync(int id);
        Task<Afiliado> ReactivarAsync(int id);

    }
}