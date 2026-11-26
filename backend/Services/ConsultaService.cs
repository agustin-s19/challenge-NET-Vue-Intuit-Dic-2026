using ConsultasPlan.Data;
using ConsultasPlan.Exceptions;
using ConsultasPlan.Models;
using ConsultasPlan.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using ConsultasPlan.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace ConsultasPlan.Services
{
    public class ConsultaService : IConsultaService
    {
        private const string EspecialidadNutricion = "Nutrición";
        private const int LimiteConsultasNutricionAnual = 6;
        private const int HorasMinimasCancelacion = 24;

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

            if (afiliado.Bloqueado)
                throw new BusinessRuleException("El afiliado se encuentra bloqueado para agendar consultas.");

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

        private async Task ValidarLimiteNutricionAsync(int afiliadoId, int anio)
        {
            var usadas = await _context.Consultas.CountAsync(c =>
                c.AfiliadoId == afiliadoId &&
                c.Profesional.Especialidad == EspecialidadNutricion &&
                c.FechaHora.Year == anio &&
                c.Estado != EstadoConsulta.Cancelado);

            if (usadas >= LimiteConsultasNutricionAnual)
                throw new BusinessRuleException(
                    $"El afiliado ya alcanzó el límite de {LimiteConsultasNutricionAnual} consultas de nutrición en {anio}.");
        }
    }
}