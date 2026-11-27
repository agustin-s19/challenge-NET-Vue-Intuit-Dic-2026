using ConsultasPlan.Models;

namespace ConsultasPlan.Services.Interfaces
{
    public interface IAfiliadoService
    {
        Task<List<Afiliado>> GetAllAsync();
        Task<Afiliado> GetByIdAsync(int id);
        Task<Afiliado> DesbloquearAsync(int id);
    }
}