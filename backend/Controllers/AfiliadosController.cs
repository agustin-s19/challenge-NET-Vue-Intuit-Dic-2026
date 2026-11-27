using ConsultasPlan.Data;
using ConsultasPlan.Models;
using ConsultasPlan.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ConsultasPlan.Controllers;

[ApiController]
[Route("[controller]")]
public class AfiliadosController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IAfiliadoService _afiliadoService;

    public AfiliadosController(AppDbContext context, IAfiliadoService afiliadoService)
    {
        _context = context;
        _afiliadoService = afiliadoService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool incluirInactivos = false)
    {
        return Ok(await _afiliadoService.GetAllAsync(incluirInactivos));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        return Ok(await _afiliadoService.GetByIdAsync(id));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Afiliado afiliado)
    {
        afiliado.createdAt = DateTime.UtcNow;
        afiliado.isActive = true;
        _context.Afiliados.Add(afiliado);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = afiliado.Id }, afiliado);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] Afiliado afiliado)
    {
        var existing = await _context.Afiliados.FirstOrDefaultAsync(a => a.Id == id && a.isActive);
        if (existing == null) return NotFound(new { message = $"No existe el afiliado {id}." });

        existing.NombreCompleto = afiliado.NombreCompleto;
        existing.DNI = afiliado.DNI;
        existing.Email = afiliado.Email;
        existing.Telefono = afiliado.Telefono;

        await _context.SaveChangesAsync();
        return Ok(existing);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _afiliadoService.EliminarAsync(id);
        return NoContent();
    }

    [HttpPost("{id}/desbloquear")]
    public async Task<IActionResult> Desbloquear(int id)
    {
        return Ok(await _afiliadoService.DesbloquearAsync(id));
    }

    [HttpPost("{id}/reactivar")]
    public async Task<IActionResult> Reactivar(int id)
    {
        return Ok(await _afiliadoService.ReactivarAsync(id));
    }
}