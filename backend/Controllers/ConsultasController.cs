using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ConsultasPlan.Data;
using ConsultasPlan.Models;
using ConsultasPlan.Exceptions;
using ConsultasPlan.Services.Interfaces;

namespace ConsultasPlan.Controllers;

[ApiController]
[Route("[controller]")]
public class ConsultasController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConsultaService _consultaService;

    public ConsultasController(AppDbContext context, IConsultaService consultaService)
    {
        _context = context;
        _consultaService = consultaService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var consultas = await _context.Consultas
            .Include(c => c.Afiliado)
            .Include(c => c.Profesional)
            .ToListAsync();
        return Ok(consultas);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var consulta = await _context.Consultas
            .Include(c => c.Afiliado)
            .Include(c => c.Profesional)
            .FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new NotFoundException($"No existe la consulta {id}.");
        return Ok(consulta);
    }

    [HttpPost]
    public async Task<IActionResult> CrearConsulta([FromBody] Consulta consulta)
    {
        var creada = await _consultaService.CrearAsync(consulta);
        return CreatedAtAction(nameof(GetById), new { id = creada.Id }, creada);
    }

    [HttpPost("cancelar/{id}")]
    public async Task<IActionResult> CancelarConsulta(int id)
    {
        return Ok(await _consultaService.CancelarAsync(id));
    }

    [HttpPost("{id}/inasistencia")]
    public async Task<IActionResult> MarcarInasistencia(int id)
    {
        return Ok(await _consultaService.MarcarInasistenciaAsync(id));
    }

    [HttpPost("{id}/atencion")]
    public async Task<IActionResult> RegistrarAtencion(int id)
    {
        return Ok(await _consultaService.RegistrarAtencionAsync(id));
    }

    [HttpPut("{id}/estado")]
    public async Task<IActionResult> ActualizarEstado(int id, [FromBody] ActualizarEstadoRequest request)
    {
        return Ok(await _consultaService.CambiarEstadoAsync(id, request.Estado));
    }
}

public class ActualizarEstadoRequest
{
    public EstadoConsulta Estado { get; set; }
}