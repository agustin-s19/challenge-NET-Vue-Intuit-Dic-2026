using System.Globalization;
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

        // ===== Alta =====

        public async Task<Consulta> CrearAsync(Consulta consulta)
        {
            if (consulta.FechaHora <= DateTime.Now)
                throw new BusinessRuleException("No se puede agendar una consulta en una fecha u hora pasada.");

            var afiliado = await _context.Afiliados.FirstOrDefaultAsync(a => a.Id == consulta.AfiliadoId && a.isActive)
                ?? throw new NotFoundException("Afiliado no encontrado o dado de baja.");

            ValidarBloqueo(afiliado);

            var profesional = await _context.Profesionales.FindAsync(consulta.ProfesionalId)
                ?? throw new NotFoundException("Profesional no encontrado.");

            var consultaConflicto = await _context.Consultas.AnyAsync(c =>
                c.ProfesionalId == consulta.ProfesionalId &&
                c.FechaHora == consulta.FechaHora &&
                c.Estado != EstadoConsulta.Cancelado);
            if (consultaConflicto)
                throw new BusinessRuleException("El profesional ya tiene una consulta en ese horario.");

            if (profesional.Especialidad == EspecialidadNutricion &&
                consulta.Modalidad == ModalidadConsulta.Presencial)
            {
                await ValidarCupoNutricionAsync(afiliado.Id, consulta.FechaHora);
            }

            consulta.FechaCreacion = DateTime.UtcNow;
            consulta.Estado = EstadoConsulta.Pendiente;
            _context.Consultas.Add(consulta);
            await _context.SaveChangesAsync();
            return consulta;
        }

        // ===== Cambios de estado =====

        public async Task<Consulta> ConfirmarAsync(int id)
        {
            var consulta = await ObtenerConsultaAsync(id);
            consulta.ValidarTransicion(EstadoConsulta.Confirmado);

            if (consulta.FechaHora <= DateTime.Now)
                throw new BusinessRuleException("No se puede confirmar una consulta que ya pasó.");

            consulta.Estado = EstadoConsulta.Confirmado;
            await _context.SaveChangesAsync();
            return consulta;
        }

        public async Task<Consulta> CancelarAsync(int id)
        {
            var consulta = await ObtenerConsultaAsync(id);
            consulta.ValidarTransicion(EstadoConsulta.Cancelado);

            if ((consulta.FechaHora - DateTime.Now).TotalHours < HorasMinimasCancelacion)
                throw new BusinessRuleException(
                    $"No se puede cancelar la consulta: solo se permite hasta {HorasMinimasCancelacion} horas antes del turno.");

            consulta.Estado = EstadoConsulta.Cancelado;
            await _context.SaveChangesAsync();
            return consulta;
        }

        public async Task<Consulta> RegistrarAtencionAsync(int id)
        {
            var consulta = await ObtenerConsultaAsync(id);
            consulta.ValidarTransicion(EstadoConsulta.Atendido);

            if (consulta.FechaHora > DateTime.Now)
                throw new BusinessRuleException("No se puede registrar la atención de una consulta que todavía no ocurrió.");

            consulta.Estado = EstadoConsulta.Atendido;
            await _context.SaveChangesAsync();
            return consulta;
        }

        public async Task<Consulta> MarcarInasistenciaAsync(int id)
        {
            var consulta = await ObtenerConsultaAsync(id);
            consulta.ValidarTransicion(EstadoConsulta.Inasistencia);

            if (consulta.FechaHora > DateTime.Now)
                throw new BusinessRuleException("No se puede registrar una inasistencia sobre una consulta futura.");

            consulta.Estado = EstadoConsulta.Inasistencia;
            await _context.SaveChangesAsync();

            var afiliado = consulta.Afiliado
                ?? throw new NotFoundException("La consulta no tiene un afiliado asociado.");

            await AplicarBloqueoSiCorrespondeAsync(afiliado);

            return consulta;
        }

        // Punto de entrada genérico (select "Cambiar estado"):
        // delega en la acción específica para que se apliquen las mismas reglas.
        public Task<Consulta> CambiarEstadoAsync(int id, EstadoConsulta nuevo)
        {
            return nuevo switch
            {
                EstadoConsulta.Confirmado => ConfirmarAsync(id),
                EstadoConsulta.Cancelado => CancelarAsync(id),
                EstadoConsulta.Atendido => RegistrarAtencionAsync(id),
                EstadoConsulta.Inasistencia => MarcarInasistenciaAsync(id),
                _ => throw new BusinessRuleException($"No se puede volver una consulta a estado {nuevo}.")
            };
        }

        // ===== Privados =====

        private async Task<Consulta> ObtenerConsultaAsync(int id)
        {
            return await _context.Consultas
                .Include(c => c.Afiliado)
                .Include(c => c.Profesional)
                .FirstOrDefaultAsync(c => c.Id == id)
                ?? throw new NotFoundException($"No existe la consulta {id}.");
        }

        private void ValidarBloqueo(Afiliado afiliado)
        {
            if (!afiliado.Bloqueado) return;

            var vence = afiliado.FechaBloqueo?.AddDays(DiasBloqueo);

            if (vence.HasValue && vence.Value <= DateTime.Now)
            {
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

        private async Task ValidarCupoNutricionAsync(int afiliadoId, DateTime fecha)
        {
            var fechasDelAnio = await _context.Consultas
                .Where(c =>
                    c.AfiliadoId == afiliadoId &&
                    c.Profesional!.Especialidad == EspecialidadNutricion &&
                    c.Modalidad == ModalidadConsulta.Presencial &&
                    c.FechaHora.Year == fecha.Year &&
                    c.Estado != EstadoConsulta.Cancelado)
                .Select(c => c.FechaHora)
                .ToListAsync();

            if (fechasDelAnio.Count >= LimiteConsultasNutricionAnual)
                throw new BusinessRuleException(
                    $"El afiliado ya usó las {LimiteConsultasNutricionAnual} consultas presenciales de nutrición de {fecha.Year}.");

            if (fechasDelAnio.Any(f => f.Month == fecha.Month))
            {
                var mes = fecha.ToString("MMMM yyyy", new CultureInfo("es-AR"));
                throw new BusinessRuleException(
                    $"El afiliado ya tiene una consulta presencial de nutrición en {mes}. El plan permite una por mes.");
            }
        }
    }
}