using AutoMapper;
using EducationalPlataform.Data;
using EducationalPlataform.DTOs;
using EducationalPlataform.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EducationalPlataform.Models.Enums;


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

    [HttpGet("students")]
    public ActionResult<IEnumerable<UserReadDto>> GetStudents()
    {
        var students = _context.Users
            .Where(u => u.Profile == UserProfile.Student)
            .ToList();

        var studentsDto = _mapper.Map<List<UserReadDto>>(students);
        return Ok(studentsDto);
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

    
    [HttpPost("students")]
    public ActionResult<UserReadDto> CreateStudent([FromBody] StudentCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.UserName))
            return BadRequest("Nome é obrigatório.");

        var student = new User
        {
            UserName = dto.UserName,
            UserEmail = dto.UserEmail,
            CPF = dto.CPF,
            BirthDate = dto.BirthDate,
            Profile = UserProfile.Student,
            Role = "Student"
        };

        _context.Users.Add(student);
        _context.SaveChanges();

        // cria a matrícula do aluno no curso
        var enrollment = new CourseEnrollment
        {
            UserId = student.Id,
            CourseId = dto.CourseId,
            Status = dto.Status,
            ProgressPercentage = 0
        };

        _context.CourseEnrollments.Add(enrollment);
        _context.SaveChanges();

        var studentDto = _mapper.Map<UserReadDto>(student);
        return CreatedAtAction(nameof(GetById), new { id = student.Id }, studentDto);
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
