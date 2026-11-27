using ConsultasPlan.Data;
using ConsultasPlan.Exceptions;
using ConsultasPlan.Models;
using ConsultasPlan.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ConsultasPlan.Services
{
    public class AfiliadoService : IAfiliadoService
    {
        private const string EspecialidadNutricion = "Nutrición";

        private readonly AppDbContext _context;

        public AfiliadoService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Afiliado>> GetAllAsync(bool incluirInactivos = false)
        {
            var afiliados = await _context.Afiliados
                .Where(a => incluirInactivos || a.isActive)
                .ToListAsync();

            var usadas = await ContarConsultasNutricionAsync(DateTime.Now.Year);

            foreach (var a in afiliados)
                a.ConsultasUsadas = usadas.GetValueOrDefault(a.Id);

            return afiliados;
        }

        public async Task<Afiliado> GetByIdAsync(int id)
        {
            var afiliado = await ObtenerActivoAsync(id);

            var usadas = await ContarConsultasNutricionAsync(DateTime.Now.Year);
            afiliado.ConsultasUsadas = usadas.GetValueOrDefault(afiliado.Id);

            return afiliado;
        }

        public async Task<Afiliado> DesbloquearAsync(int id)
        {
            var afiliado = await ObtenerActivoAsync(id);

            if (!afiliado.Bloqueado)
                throw new BusinessRuleException("El afiliado no está bloqueado.");

            // FechaBloqueo se conserva: marca desde cuándo se vuelven a contar inasistencias.
            afiliado.Bloqueado = false;
            await _context.SaveChangesAsync();
            return afiliado;
        }

        public async Task EliminarAsync(int id)
        {
            var afiliado = await ObtenerActivoAsync(id);

            var tieneTurnosFuturos = await _context.Consultas.AnyAsync(c =>
                c.AfiliadoId == id &&
                c.FechaHora > DateTime.Now &&
                (c.Estado == EstadoConsulta.Pendiente || c.Estado == EstadoConsulta.Confirmado));

            if (tieneTurnosFuturos)
                throw new BusinessRuleException(
                    "El afiliado tiene turnos pendientes o confirmados. Cancelalos antes de darlo de baja.");

            // Baja lógica: se conserva el registro para no perder el historial de consultas.
            afiliado.isActive = false;
            await _context.SaveChangesAsync();
        }

        public async Task<Afiliado> ReactivarAsync(int id)
        {
            var afiliado = await _context.Afiliados.FindAsync(id)
                ?? throw new NotFoundException($"No existe el afiliado {id}.");

            if (afiliado.isActive)
                throw new BusinessRuleException("El afiliado ya está activo.");

            afiliado.isActive = true;
            await _context.SaveChangesAsync();
            return afiliado;
        }
        private async Task<Afiliado> ObtenerActivoAsync(int id)
        {
            return await _context.Afiliados.FirstOrDefaultAsync(a => a.Id == id && a.isActive)
                ?? throw new NotFoundException($"No existe el afiliado {id}.");
        }

        private async Task<Dictionary<int, int>> ContarConsultasNutricionAsync(int anio)
        {
            return await _context.Consultas
                .Where(c =>
                    c.Profesional!.Especialidad == EspecialidadNutricion &&
                    c.Modalidad == ModalidadConsulta.Presencial &&
                    c.FechaHora.Year == anio &&
                    c.Estado != EstadoConsulta.Cancelado &&
                    c.AfiliadoId != null)
                .GroupBy(c => c.AfiliadoId!.Value)
                .Select(g => new { AfiliadoId = g.Key, Cantidad = g.Count() })
                .ToDictionaryAsync(x => x.AfiliadoId, x => x.Cantidad);
        }
    }
}