using AutoMapper;
using EducationalPlataform.Data;
using EducationalPlataform.DTOs;
using EducationalPlataform.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly EducationalPlataformContext _context;
    private readonly IMapper _mapper;

    public UsersController(EducationalPlataformContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    [HttpGet]
    public ActionResult<IEnumerable<UserReadDto>> GetAll()
    {
        var users = _context.Users.ToList();
        var usersDto = _mapper.Map<List<UserReadDto>>(users);
        return Ok(usersDto);
    }

    [HttpGet("{id}")]
    public ActionResult<UserReadDto> GetById(int id)
    {
        var user = _context.Users.Find(id);
        if (user == null)
            throw new ArgumentException($"Course with id {id} not found");
        var userDto = _mapper.Map<UserReadDto>(user);
        return Ok(userDto);
    }


    [HttpPost]
    public ActionResult<UserReadDto> Create([FromBody] UserCreateDto dto)
    {
        var user = _mapper.Map<User>(dto);
        _context.Users.Add(user);
        _context.SaveChanges();

        var userReadDto = _mapper.Map<UserReadDto>(user);
        return CreatedAtAction(nameof(GetById), new { id = user.Id }, userReadDto);
    }

    [HttpPut("{id}")]
    public ActionResult Update(int id, [FromBody] UserUpdateDto dto)
    {
        var user = _context.Users.Find(id);
        if (user == null)
            throw new ArgumentException($"Course with id {id} not found");

        _mapper.Map(dto, user);
        _context.SaveChanges();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public ActionResult Delete(int id)
    {
        var user = _context.Users.Find(id);
        if (user == null)
            throw new ArgumentException($"Course with id {id} not found");

        _context.Users.Remove(user);
        _context.SaveChanges();
        return NoContent();
    }
}
