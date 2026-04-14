using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using server.Data;
using server.Models;

namespace server.Controllers;

[Authorize]  // 需�?JWT 认证才能访问此控制器的所有端�?[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _db;

    public UsersController(AppDbContext db)
    {
        _db = db;
    }

    // GET api/users
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var users = await _db.Users.ToListAsync();
        return Ok(users);
    }

    // GET api/users/1
    [HttpGet("{id}")] 
    public async Task<IActionResult> GetById(int id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user is null) return NotFound();
        return Ok(user);
    }

    // POST api/users
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] User user)
    {
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
    }

    // PUT api/users/1
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] User user)
    {
        if (id != user.Id) return BadRequest("用户 ID 不匹�?);
        
        var existingUser = await _db.Users.FindAsync(id);
        if (existingUser is null) return NotFound();
        
        existingUser.Name = user.Name;
        existingUser.Email = user.Email;
        
        await _db.SaveChangesAsync();
        return Ok(existingUser);
    }

    // DELETE api/users/1
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user is null) return NotFound();
        _db.Users.Remove(user);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
