using DungeonWorld.API.DTOs;
using DungeonWorld.Core.Entities;
using DungeonWorld.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DungeonWorld.API.Controllers;

[ApiController]
[Route("api/catalog")]
public class CatalogController : ControllerBase
{
    private readonly DungeonWorldDbContext _db;

    public CatalogController(DungeonWorldDbContext db)
    {
        _db = db;
    }

    // --- Public reads ---

    [AllowAnonymous]
    [HttpGet("items")]
    public async Task<IActionResult> GetItems() =>
        Ok(await _db.Items.OrderBy(i => i.Name).Select(i => ToDto(i)).ToListAsync());

    [AllowAnonymous]
    [HttpGet("spells")]
    public async Task<IActionResult> GetSpells() =>
        Ok(await _db.Spells.OrderBy(s => s.Name).Select(s => ToDto(s)).ToListAsync());

    [AllowAnonymous]
    [HttpGet("commands")]
    public async Task<IActionResult> GetCommands() =>
        Ok(await _db.Commands.OrderBy(c => c.Category).ThenBy(c => c.Name).Select(c => ToDto(c)).ToListAsync());

    [AllowAnonymous]
    [HttpGet("adventures")]
    public async Task<IActionResult> GetAdventures() =>
        Ok(await _db.Adventures.OrderBy(a => a.BookTitle).Select(a => ToDto(a)).ToListAsync());

    [AllowAnonymous]
    [HttpGet("adventures/{bookTitle}")]
    public async Task<IActionResult> GetAdventure(string bookTitle)
    {
        var adventure = await _db.Adventures.FirstOrDefaultAsync(a => a.BookTitle == bookTitle);
        return adventure == null
            ? NotFound(new { error = $"Adventure '{bookTitle}' not found." })
            : Ok(ToDto(adventure));
    }

    // --- Admin management ---

    [Authorize]
    [HttpPost("items")]
    public async Task<IActionResult> AddItem(ItemRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { error = "Name is required." });

        var item = new GameItem
        {
            Name = request.Name.Trim(),
            Type = request.Type,
            Description = request.Description,
            Rarity = request.Rarity,
            BookTitle = request.BookTitle,
            SectionNumber = request.SectionNumber,
            RequiredLevel = request.RequiredLevel,
            RequiredSkill = request.RequiredSkill,
            RequiredStamina = request.RequiredStamina,
            RequiredLuck = request.RequiredLuck,
            Effects = request.Effects
        };

        _db.Items.Add(item);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetItems), ToDto(item));
    }

    [Authorize]
    [HttpPost("spells")]
    public async Task<IActionResult> AddSpell(SpellRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { error = "Name is required." });

        var spell = new Spell
        {
            Name = request.Name.Trim(),
            Type = request.Type,
            Description = request.Description,
            Effects = request.Effects,
            BookTitle = request.BookTitle,
            SectionNumber = request.SectionNumber,
            RequiredLevel = request.RequiredLevel,
            RequiredSkill = request.RequiredSkill,
            RequiredStamina = request.RequiredStamina,
            RequiredLuck = request.RequiredLuck
        };

        _db.Spells.Add(spell);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetSpells), ToDto(spell));
    }

    [Authorize]
    [HttpPost("commands")]
    public async Task<IActionResult> AddCommand(GameCommandRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { error = "Name is required." });

        var command = new GameCommand
        {
            Name = request.Name.Trim().ToUpperInvariant(),
            Aliases = request.Aliases ?? Array.Empty<string>(),
            Description = request.Description,
            Usage = request.Usage,
            Category = request.Category
        };

        _db.Commands.Add(command);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetCommands), ToDto(command));
    }

    [Authorize]
    [HttpPost("adventures")]
    public async Task<IActionResult> AddAdventure(AdventureCatalogRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.BookTitle))
            return BadRequest(new { error = "BookTitle is required." });

        var adventure = new Adventure
        {
            BookTitle = request.BookTitle,
            SectionCount = request.SectionCount,
            Description = request.Description,
            MedallionTitle = request.MedallionTitle,
            MedallionDescription = request.MedallionDescription
        };

        _db.Adventures.Add(adventure);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetAdventure), new { bookTitle = adventure.BookTitle }, ToDto(adventure));
    }

    [Authorize]
    [HttpPut("items/{id:guid}")]
    public async Task<IActionResult> UpdateItem(Guid id, ItemRequest request)
    {
        var item = await _db.Items.FindAsync(id);
        if (item == null) return NotFound(new { error = "Item not found." });

        item.Name = request.Name.Trim();
        item.Type = request.Type;
        item.Description = request.Description;
        item.Rarity = request.Rarity;
        item.BookTitle = request.BookTitle;
        item.SectionNumber = request.SectionNumber;
        item.RequiredLevel = request.RequiredLevel;
        item.RequiredSkill = request.RequiredSkill;
        item.RequiredStamina = request.RequiredStamina;
        item.RequiredLuck = request.RequiredLuck;
        item.Effects = request.Effects;

        await _db.SaveChangesAsync();
        return Ok(ToDto(item));
    }

    [Authorize]
    [HttpPut("spells/{id:guid}")]
    public async Task<IActionResult> UpdateSpell(Guid id, SpellRequest request)
    {
        var spell = await _db.Spells.FindAsync(id);
        if (spell == null) return NotFound(new { error = "Spell not found." });

        spell.Name = request.Name.Trim();
        spell.Type = request.Type;
        spell.Description = request.Description;
        spell.Effects = request.Effects;
        spell.BookTitle = request.BookTitle;
        spell.SectionNumber = request.SectionNumber;
        spell.RequiredLevel = request.RequiredLevel;
        spell.RequiredSkill = request.RequiredSkill;
        spell.RequiredStamina = request.RequiredStamina;
        spell.RequiredLuck = request.RequiredLuck;

        await _db.SaveChangesAsync();
        return Ok(ToDto(spell));
    }

    [Authorize]
    [HttpPut("commands/{id:guid}")]
    public async Task<IActionResult> UpdateCommand(Guid id, GameCommandRequest request)
    {
        var command = await _db.Commands.FindAsync(id);
        if (command == null) return NotFound(new { error = "Command not found." });

        command.Name = request.Name.Trim().ToUpperInvariant();
        command.Aliases = request.Aliases ?? Array.Empty<string>();
        command.Description = request.Description;
        command.Usage = request.Usage;
        command.Category = request.Category;

        await _db.SaveChangesAsync();
        return Ok(ToDto(command));
    }

    [Authorize]
    [HttpPut("adventures/{id:guid}")]
    public async Task<IActionResult> UpdateAdventure(Guid id, AdventureCatalogRequest request)
    {
        var adventure = await _db.Adventures.FindAsync(id);
        if (adventure == null) return NotFound(new { error = "Adventure not found." });

        adventure.BookTitle = request.BookTitle;
        adventure.SectionCount = request.SectionCount;
        adventure.Description = request.Description;
        adventure.MedallionTitle = request.MedallionTitle;
        adventure.MedallionDescription = request.MedallionDescription;

        await _db.SaveChangesAsync();
        return Ok(ToDto(adventure));
    }

    [Authorize]
    [HttpDelete("items/{id:guid}")]
    public async Task<IActionResult> DeleteItem(Guid id)
    {
        var item = await _db.Items.FindAsync(id);
        if (item == null) return NotFound(new { error = "Item not found." });
        _db.Items.Remove(item);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [Authorize]
    [HttpDelete("spells/{id:guid}")]
    public async Task<IActionResult> DeleteSpell(Guid id)
    {
        var spell = await _db.Spells.FindAsync(id);
        if (spell == null) return NotFound(new { error = "Spell not found." });
        _db.Spells.Remove(spell);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [Authorize]
    [HttpDelete("commands/{id:guid}")]
    public async Task<IActionResult> DeleteCommand(Guid id)
    {
        var command = await _db.Commands.FindAsync(id);
        if (command == null) return NotFound(new { error = "Command not found." });
        _db.Commands.Remove(command);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [Authorize]
    [HttpDelete("adventures/{id:guid}")]
    public async Task<IActionResult> DeleteAdventure(Guid id)
    {
        var adventure = await _db.Adventures.FindAsync(id);
        if (adventure == null) return NotFound(new { error = "Adventure not found." });
        _db.Adventures.Remove(adventure);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // --- Mapping ---

    private static ItemResponse ToDto(GameItem i) => new(
        i.Id, i.Name, i.Type, i.Description, i.Rarity, i.BookTitle, i.SectionNumber,
        i.RequiredLevel, i.RequiredSkill, i.RequiredStamina, i.RequiredLuck, i.Effects);

    private static SpellResponse ToDto(Spell s) => new(
        s.Id, s.Name, s.Type, s.Description, s.Effects, s.BookTitle, s.SectionNumber,
        s.RequiredLevel, s.RequiredSkill, s.RequiredStamina, s.RequiredLuck);

    private static GameCommandResponse ToDto(GameCommand c) => new(
        c.Id, c.Name, c.Aliases, c.Description, c.Usage, c.Category);

    private static AdventureCatalogResponse ToDto(Adventure a) => new(
        a.Id, a.BookTitle, a.SectionCount, a.Description, a.MedallionTitle, a.MedallionDescription);
}
