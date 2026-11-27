using ConsultasPlan.Data;
using ConsultasPlan.Exceptions;
using ConsultasPlan.Models;
using ConsultasPlan.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ConsultasPlan.Services
{
    public class ConsultaService : IConsultaService
    {
        private const string EspecialidadNutricion = "Nutrición";
        private const int LimiteConsultasNutricionAnual = 6;
        private const int HorasMinimasCancelacion = 24;
        private const int LimiteInasistencias = 3;
        private const int DiasBloqueo = 30;

        private readonly AppDbContext _context;

        public ConsultaService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Consulta> CrearAsync(Consulta consulta)
        {
            if (consulta.FechaHora <= DateTime.Now)
                throw new BusinessRuleException("No se puede agendar una consulta en una fecha u hora pasada.");

            var afiliado = await _context.Afiliados.FindAsync(consulta.AfiliadoId)
                ?? throw new NotFoundException("Afiliado no encontrado.");

            ValidarBloqueo(afiliado);

            var profesional = await _context.Profesionales.FindAsync(consulta.ProfesionalId)
                ?? throw new NotFoundException("Profesional no encontrado.");

            var consultaConflicto = await _context.Consultas.AnyAsync(c =>
                c.ProfesionalId == consulta.ProfesionalId &&
                c.FechaHora == consulta.FechaHora &&
                c.Estado != EstadoConsulta.Cancelado);
            if (consultaConflicto)
                throw new BusinessRuleException("El profesional ya tiene una consulta en ese horario.");

            if (profesional.Especialidad == EspecialidadNutricion)
                await ValidarLimiteNutricionAsync(afiliado.Id, consulta.FechaHora.Year);

            consulta.FechaCreacion = DateTime.UtcNow;
            consulta.Estado = EstadoConsulta.Pendiente;
            _context.Consultas.Add(consulta);
            await _context.SaveChangesAsync();
            return consulta;
        }

        public async Task<Consulta> MarcarInasistenciaAsync(int id)
        {
            var consulta = await _context.Consultas
                .Include(c => c.Afiliado)
                .FirstOrDefaultAsync(c => c.Id == id)
                ?? throw new NotFoundException($"No existe la consulta {id}.");

            if (consulta.Estado != EstadoConsulta.Pendiente && consulta.Estado != EstadoConsulta.Confirmado)
                throw new BusinessRuleException(
                    $"No se puede marcar inasistencia en una consulta en estado {consulta.Estado}.");

            if (consulta.FechaHora > DateTime.Now)
                throw new BusinessRuleException("No se puede registrar una inasistencia sobre una consulta futura.");

            consulta.Estado = EstadoConsulta.Inasistencia;
            await _context.SaveChangesAsync();

            var afiliado = consulta.Afiliado
                ?? throw new NotFoundException("La consulta no tiene un afiliado asociado.");

            await AplicarBloqueoSiCorrespondeAsync(afiliado);

            return consulta;
        }

        // ===== Reglas privadas =====

        private void ValidarBloqueo(Afiliado afiliado)
        {
            if (!afiliado.Bloqueado) return;

            var vence = afiliado.FechaBloqueo?.AddDays(DiasBloqueo);

            if (vence.HasValue && vence.Value <= DateTime.Now)
            {
                // Bloqueo vencido: se libera automáticamente.
                // FechaBloqueo se conserva como referencia del último bloqueo.
                afiliado.Bloqueado = false;
                return;
            }

            if (vence.HasValue)
                throw new BusinessRuleException(
                    $"El afiliado está bloqueado por inasistencias hasta el {vence.Value:dd/MM/yyyy}.");

            throw new BusinessRuleException(
                "El afiliado se encuentra bloqueado. Debe ser desbloqueado por un administrador.");
        }

        private async Task AplicarBloqueoSiCorrespondeAsync(Afiliado afiliado)
        {
            if (afiliado.Bloqueado) return;

            var desde = afiliado.FechaBloqueo ?? DateTime.MinValue;

            var inasistencias = await _context.Consultas.CountAsync(c =>
                c.AfiliadoId == afiliado.Id &&
                c.Estado == EstadoConsulta.Inasistencia &&
                c.FechaHora > desde);

            if (inasistencias >= LimiteInasistencias)
            {
                afiliado.Bloqueado = true;
                afiliado.FechaBloqueo = DateTime.Now;
                await _context.SaveChangesAsync();
            }
        }

        private async Task ValidarLimiteNutricionAsync(int afiliadoId, int anio)
        {
            var usadas = await _context.Consultas.CountAsync(c =>
                c.AfiliadoId == afiliadoId &&
                c.Profesional!.Especialidad == EspecialidadNutricion &&
                c.FechaHora.Year == anio &&
                c.Estado != EstadoConsulta.Cancelado);

            if (usadas >= LimiteConsultasNutricionAnual)
                throw new BusinessRuleException(
                    $"El afiliado ya alcanzó el límite de {LimiteConsultasNutricionAnual} consultas de nutrición en {anio}.");
        }
    }
}